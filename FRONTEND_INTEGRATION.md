# SkillMind — Frontend Integration: Stripe Payments & Subscriptions

---

## Overview

The subscription and payment UI for SkillMind uses **Stripe Elements** (custom payment UI — not Stripe's hosted Checkout page). The backend handles all Stripe communication — the frontend only talks to the SkillMind API Gateway.

### Registration + Plan Selection Flow (Option A)

The user picks a plan **during registration**, but the actual Stripe checkout happens **after** email verification and login — because a JWT is required to call the payment API.

```
┌─────────────────────────────────────────────────────────────────────┐
│  1. /register                                                        │
│     User fills in name, email, password                              │
│     + picks plan: [ Free ] or [ Pro ]                                │
│     → POST /api/v1/auth/register                                     │
│     → localStorage.set('pendingPlan', 'pro' | 'free')                │
│     → redirect to /verify-email (show "check your inbox" screen)     │
├─────────────────────────────────────────────────────────────────────┤
│  2. /verify-email                                                    │
│     User enters OTP code from email                                  │
│     → POST /api/v1/auth/verify (or however your verify endpoint works│
│     → redirect to /login                                             │
├─────────────────────────────────────────────────────────────────────┤
│  3. /login                                                           │
│     User logs in → gets JWT                                          │
│     → read localStorage.get('pendingPlan')                           │
│       'free' or null → clear key → redirect to /dashboard            │
│       'pro'          → clear key → redirect to /checkout?plan=pro    │
├─────────────────────────────────────────────────────────────────────┤
│  4. /checkout?plan=pro                                               │
│     Auto-calls POST /api/v1/payment/create-subscription              │
│     Mounts Stripe Elements <PaymentElement> (custom UI)              │
│     User pays → stripe.confirmPayment() → redirects to /return       │
├─────────────────────────────────────────────────────────────────────┤
│  5. /return?payment_intent=pi_xxx&...                                │
│     stripe.retrievePaymentIntent(clientSecret) → status = 'succeeded'│
│     → redirect to /dashboard (now Active)                            │
└─────────────────────────────────────────────────────────────────────┘
```

> **Why `localStorage`?** The plan choice needs to survive the full register → verify email → login redirect cycle. It's cleared immediately after login so it only fires once.

---

## Base URL & Auth

```
API Gateway Base URL: http://localhost:3000
All versioned routes:  /api/v1/...
Auth:                  Bearer token in every request (except the webhook)
  Authorization: Bearer <jwt>
```

---

## TypeScript Types

```typescript
// ─── Enums ──────────────────────────────────────────────────────────────────

type SubscriptionStatus =
  | 'Free'        // Default on registration — no Stripe subscription yet
  | 'Active'      // Paid and in good standing
  | 'Trialing'    // Free trial period is active
  | 'PastDue'     // Payment failed, grace period is running
  | 'Suspended'   // Grace period expired without payment — access blocked
  | 'Canceled'    // Explicitly canceled by user
  | 'Deleted';    // Removed after suspension with no recovery

type PaymentFailureReason =
  | 'InsufficientFunds'
  | 'CardDeclined'
  | 'NetworkError'
  | 'BankDelay'
  | 'Expired'
  | 'Unknown';

// ─── Response shapes ─────────────────────────────────────────────────────────

interface Subscription {
  id: string;                             // UUID
  userId: string;                         // UUID

  // These fields are null when subscriptionStatus === 'Free'
  stripeSubscriptionId: string | null;    // e.g. "sub_xxx"
  stripeCustomerId: string | null;        // e.g. "cus_xxx"
  stripePriceId: string | null;           // e.g. "price_xxx"
  stripeLookupKey: string | null;         // e.g. "pro_monthly"

  // Set when user starts checkout but hasn't completed payment yet.
  // On next login: if set, redirect to /checkout?plan={intendedPlan}
  intendedPlan: string | null;            // e.g. "pro_monthly"

  status: string;                         // Raw Stripe string: "free" | "active" | "past_due" | "canceled"
  subscriptionStatus: SubscriptionStatus; // Typed enum — use this for ALL UI logic
  plan: string;                           // "free" | "pro_monthly" | etc.
  currentPeriodStart: string;             // ISO 8601 UTC (zero value for free tier)
  currentPeriodEnd: string;               // ISO 8601 UTC (zero value for free tier)
  canceledAt: string | null;
  trialEnd: string | null;

  // Grace period (populated when subscriptionStatus === 'PastDue' | 'Suspended')
  isInGracePeriod: boolean;
  gracePeriodStart: string | null;        // ISO 8601 UTC
  gracePeriodEnd: string | null;          // ISO 8601 UTC — deadline to pay before suspension

  // Retry tracking (informational)
  retryAttemptCount: number;              // How many auto-retries have been attempted
  nextRetryAt: string | null;             // ISO 8601 UTC — when the next auto-retry fires
  lastFailureAt: string | null;           // ISO 8601 UTC
  lastFailureReason: PaymentFailureReason | null;

  createdAt: string;
  updatedAt: string;
}

interface SubscriptionClientSecretResponse {
  clientSecret: string;      // Pass to stripe.confirmPayment() as the Elements clientSecret
  subscriptionId: string;    // Stripe subscription ID ("sub_xxx")
}

interface PortalSessionResponse {
  url: string;           // Redirect user to this URL
}
```

---

## API Endpoints

### Summary

| Method | Path | Auth | Purpose |
|--------|------|------|---------|
| `POST` | `/api/v1/payment/create-subscription` | ✅ JWT | Start new subscription — returns Elements `clientSecret` |
| `POST` | `/api/v1/payment/create-portal-session` | ✅ JWT | Open Stripe Billing Portal |
| `GET`  | `/api/v1/payment/subscription` | ✅ JWT | Get current user's subscription |
| `POST` | `/api/v1/payment/webhook` | ❌ None | Stripe server-to-server only — no action needed |

---

### 1. Create Subscription (Stripe Elements)

Creates a Stripe Customer + incomplete Subscription and returns the PaymentIntent `clientSecret` for Stripe Elements.

```
POST /api/v1/payment/create-subscription
Authorization: Bearer <jwt>
Content-Type: application/json

Body:
{
  "lookupKey": "pro_monthly"   // The Stripe price lookup key for the plan
}

Response 200:
{
  "clientSecret": "pi_xxx_secret_xxx",
  "subscriptionId": "sub_xxx"
}
```

**What the backend does:**
1. Looks up the Stripe Price by the `lookupKey`.
2. Creates (or reuses) a Stripe Customer for this user.
3. Sets `intendedPlan` on the user's subscription record (persists across browser close).
4. Creates an **incomplete** Stripe Subscription with `payment_behavior: default_incomplete`.
5. Returns the PaymentIntent `clientSecret` from the subscription's latest invoice.

**Frontend flow:**
1. Call this endpoint when the user clicks "Subscribe".
2. Use the `clientSecret` to confirm payment with `stripe.confirmPayment()`.
3. On return from Stripe, check `payment_intent` status — `"succeeded"` → redirect to dashboard.

---

### 2. Open Billing Portal (Manage Subscription)

Lets users update their card, cancel, or view invoice history via Stripe's hosted portal.

```
POST /api/v1/payment/create-portal-session
Authorization: Bearer <jwt>
Content-Type: application/json

Body: {} (empty)

Response 200:
{
  "url": "https://billing.stripe.com/session/xxx"
}
```

**Flow:**
1. Call this when the user clicks "Manage Billing" or "Update Payment Method".
2. `window.location.href = response.url` — Stripe handles everything from here.
3. Stripe redirects back to your site when the user finishes.

---

### 3. Get Current Subscription

Fetches the authenticated user's subscription record. **Every user always has a record** — a `Free` tier subscription is created automatically on registration.

```
GET /api/v1/payment/subscription
Authorization: Bearer <jwt>

Response 200: Subscription   (see type above)
```

**Flow:**
1. Call on app load / dashboard mount to determine which features to unlock.
2. Use `subscriptionStatus` (the typed enum) for all UI logic — not the raw `status` string.
3. A brand-new user will get `subscriptionStatus: 'Free'` — show the free tier UI and an upgrade CTA.
4. Re-fetch after the user returns from the Billing Portal or checkout.
5. **If `intendedPlan` is non-null after login** — the user started checkout but never completed payment. Redirect them back to `/checkout?plan={intendedPlan}` automatically.

---

## Registration & Plan Selection Flow

### Step 1 — Registration page

Show a plan picker on the registration form. On submit, store the choice and register:

```typescript
// /register page

const PENDING_PLAN_KEY = 'skillmind:pendingPlan';

async function handleRegister(formData: {
  name: string; lastName: string; email: string;
  password: string; plan: 'free' | 'pro';
}) {
  const res = await fetch('/api/v1/auth/register', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(formData),
  });

  if (!res.ok) {
    const err = await res.json();
    // show error
    return;
  }

  // Store plan choice — survives the verify-email → login redirect cycle
  localStorage.setItem(PENDING_PLAN_KEY, formData.plan);

  // Redirect to email verification screen
  router.push('/verify-email');
}
```

---

### Step 2 — After login (post-JWT)

Check **both** the server-side `intendedPlan` (for returning/abandoned-checkout users) and the localStorage stored plan:

```typescript
// Call this right after receiving + storing the JWT from /auth/login

const PENDING_PLAN_KEY = 'skillmind:pendingPlan';

async function handlePostLoginRedirect(token: string) {
  // 1. Check server-side intendedPlan first — survives browser close / device change
  const subRes = await fetch('/api/v1/payment/subscription', {
    headers: { Authorization: `Bearer ${token}` },
  });
  const sub: Subscription = await subRes.json();

  if (sub.intendedPlan) {
    // User started checkout before — resume where they left off
    router.push(`/checkout?plan=${sub.intendedPlan}`);
    return;
  }

  // 2. Fall back to localStorage (first login after registration)
  const pendingPlan = localStorage.getItem(PENDING_PLAN_KEY);
  localStorage.removeItem(PENDING_PLAN_KEY); // always clear immediately — fire once

  if (pendingPlan === 'pro') {
    router.push('/checkout?plan=pro');
  } else {
    router.push('/dashboard');
  }
}
```

> `intendedPlan` is set by the backend the moment `create-subscription` is called and is cleared automatically by the webhook when payment succeeds. This means even if the user closes the browser mid-payment, they'll be redirected back to checkout on their next login.

---

### Step 3 — Checkout page (`/checkout?plan=pro`)

This page **auto-mounts** on load using **Stripe Elements** (custom payment UI):

```typescript
// /checkout page
import { loadStripe } from '@stripe/stripe-js';
import { Elements, PaymentElement, useStripe, useElements } from '@stripe/react-stripe-js';
// (or use vanilla stripe.js if not using React — see below)

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY!);

// ──────────────────────────────────────────────────────────────
// 1. Fetch clientSecret from backend on page mount
// ──────────────────────────────────────────────────────────────
const plan = new URLSearchParams(window.location.search).get('plan') ?? 'pro_monthly';

const res = await fetch('/api/v1/payment/create-subscription', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${getToken()}`,
  },
  body: JSON.stringify({ lookupKey: plan }),
});

if (!res.ok) {
  router.push('/dashboard?error=checkout_failed');
  return;
}

const { clientSecret, subscriptionId } = await res.json();
// clientSecret is a PaymentIntent secret: "pi_xxx_secret_xxx"

// ──────────────────────────────────────────────────────────────
// 2a. React — wrap in <Elements> and render <PaymentElement>
// ──────────────────────────────────────────────────────────────
// <Elements stripe={stripePromise} options={{ clientSecret }}>
//   <CheckoutForm returnUrl="http://localhost:3005/return" />
// </Elements>

// Inside <CheckoutForm>:
function CheckoutForm({ returnUrl }: { returnUrl: string }) {
  const stripe = useStripe();
  const elements = useElements();

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    if (!stripe || !elements) return;

    const { error } = await stripe.confirmPayment({
      elements,
      confirmParams: {
        return_url: returnUrl,  // e.g. 'http://localhost:3005/return'
      },
    });

    if (error) {
      // Show error.message to the user
      console.error(error.message);
    }
    // On success, Stripe redirects to return_url automatically
  }

  return (
    <form onSubmit={handleSubmit}>
      <PaymentElement />
      <button type="submit">Subscribe</button>
    </form>
  );
}

// ──────────────────────────────────────────────────────────────
// 2b. Vanilla JS alternative (no framework)
// ──────────────────────────────────────────────────────────────
const stripe = await stripePromise;
const elements = stripe!.elements({ clientSecret });
const paymentElement = elements.create('payment');
paymentElement.mount('#payment-element');

document.getElementById('pay-button')!.addEventListener('click', async () => {
  const { error } = await stripe!.confirmPayment({
    elements,
    confirmParams: { return_url: 'http://localhost:3005/return' },
  });
  if (error) console.error(error.message);
});
```

```html
<!-- Vanilla JS: container Stripe renders into -->
<div id="payment-element"></div>
<button id="pay-button">Subscribe</button>
```

> On successful payment, Stripe redirects to `return_url` with `?payment_intent=pi_xxx&payment_intent_client_secret=pi_xxx_secret_xxx&redirect_status=succeeded`. The backend upgrades the user's `Free` subscription to `Active` automatically via webhook.

---

### Step 4 — Return page (`/return?payment_intent=pi_xxx&...`)

```typescript
// /return page — confirm payment result after stripe.confirmPayment() redirect

import { loadStripe } from '@stripe/stripe-js';

const stripe = await loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY!);

const params = new URLSearchParams(window.location.search);
const clientSecret = params.get('payment_intent_client_secret');
const redirectStatus = params.get('redirect_status');

if (!clientSecret) {
  router.push('/dashboard');
  return;
}

// Confirm the final status directly from Stripe (no backend call needed)
const { paymentIntent } = await stripe!.retrievePaymentIntent(clientSecret);

switch (paymentIntent?.status) {
  case 'succeeded':
    // Payment confirmed — webhook will have already fired (or fires shortly)
    // Re-fetch subscription to get updated Active status
    router.push('/dashboard?welcome=pro');
    break;

  case 'processing':
    // Bank transfer / delayed payment — show pending UI and poll subscription
    router.push('/dashboard?message=payment_processing');
    break;

  case 'requires_payment_method':
    // Payment failed — send user back to checkout to retry
    router.push('/checkout?plan=pro&error=payment_failed');
    break;

  default:
    router.push('/checkout?plan=pro&error=unknown');
}
```

---

## Subscription Status UI Logic

| `subscriptionStatus` | What to show |
|---|---|
| `Free` | Limited feature set. Show upgrade CTA → call `create-checkout-session` with your paid plan's `lookupKey`. |
| `Active` | Full access. Show renewal date from `currentPeriodEnd`. |
| `Trialing` | Full access. Show trial end from `trialEnd`. Prompt to add a payment method before trial expires. |
| `PastDue` | ⚠️ Payment failed banner. Show grace period countdown from `gracePeriodEnd`. Show "Update Payment Method" → Billing Portal. |
| `Suspended` | 🔒 Access blocked. Hard paywall. Show "Reactivate Subscription" → new checkout session. |
| `Canceled` | Subscription ended. Show `canceledAt`. Show "Resubscribe" → new checkout session. |
| `Deleted` | Same as Canceled — more permanent. Show "Resubscribe". |

### Recommended routing helper

```typescript
function resolveAccess(sub: Subscription): 'full' | 'trial' | 'free' | 'payment-issue' | 'blocked' {
  switch (sub.subscriptionStatus) {
    case 'Active':    return 'full';
    case 'Trialing':  return 'trial';
    case 'Free':      return 'free';
    case 'PastDue':   return 'payment-issue';
    case 'Suspended':
    case 'Canceled':
    case 'Deleted':   return 'blocked';
  }
}
```

---

## Grace Period Banner (PastDue)

When `isInGracePeriod === true`, show a **persistent banner**. Recommended content:

```
⚠️ Your payment failed. Update your payment method before [gracePeriodEnd]
   to avoid losing access.

   Reason: [human-readable lastFailureReason]
   Next auto-retry: [nextRetryAt]

   [Update Payment Method →]    ← opens Billing Portal
```

### `lastFailureReason` human-readable labels

```typescript
const failureReasonLabels: Record<PaymentFailureReason, string> = {
  InsufficientFunds: 'Insufficient funds',
  CardDeclined:      'Card declined',
  NetworkError:      'Network error — please try again',
  BankDelay:         'Bank processing delay',
  Expired:           'Card expired',
  Unknown:           'Payment could not be processed',
};
```

### Grace period countdown helper

```typescript
function getDaysRemaining(gracePeriodEnd: string): number {
  const end = new Date(gracePeriodEnd);
  const now = new Date();
  const diff = end.getTime() - now.getTime();
  return Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
}
```

> **Backend defaults:** 3-day grace period · up to 9 automatic retries · every 8 hours.

---

## Stripe Setup

### Install

```bash
npm install @stripe/stripe-js @stripe/react-stripe-js
# or
yarn add @stripe/stripe-js @stripe/react-stripe-js
```

> `@stripe/react-stripe-js` is optional — only needed for the React `<Elements>` / `<PaymentElement>` components. Pure vanilla JS only needs `@stripe/stripe-js`.

### Environment variable

Add to your frontend `.env`:
```
NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY=pk_test_...
```

This is the **only** Stripe key the frontend ever needs. The secret key lives entirely on the server — never expose it to the browser.

> For the full checkout and return page implementation see the **Registration & Plan Selection Flow** section above.

---

## Error Handling

```typescript
// Token expired (401)
{ statusCode: 401, errorCode: "TOKEN_EXPIRED", message: "Your session has expired. Please log in again." }

// Missing / invalid auth (401)
{ statusCode: 401, errorCode: "UNAUTHORIZED", message: "Unauthorized. Please provide a valid token." }

// Server error (500)
{ statusCode: 500, message: "Internal Server Error" }
```

> **Note:** `GET /api/v1/payment/subscription` no longer returns `404` — every user gets a `Free` subscription record at registration time.

---

## Webhook

`POST /api/v1/payment/webhook` is called **by Stripe directly** (server-to-server). The backend verifies the `Stripe-Signature` header automatically. **No frontend action is needed.**

The following events are handled server-side:

| Stripe Event | Effect |
|---|---|
| `customer.subscription.created` | Upgrades the user's `Free` record to `Active` (or `Trialing`) in-place · publishes to Kafka |
| `customer.subscription.updated` | Updates plan / period / status |
| `customer.subscription.deleted` | Sets status to `Canceled` |
| `customer.subscription.trial_will_end` | Sends trial ending email |
| `invoice.payment_failed` | Sets `PastDue` · starts grace period · schedules retries · sends email |
| `invoice.payment_succeeded` | Clears grace period · sets `Active` · sends recovery email |

> **How free → paid upgrade works:** When `create-subscription` is called, the user's `userId` is embedded into the Stripe subscription metadata (and `intendedPlan` is set on the DB record). On `customer.subscription.created`, the backend finds the user's existing `Free` subscription by `userId` and upgrades it in-place, clearing `intendedPlan` — no duplicate records are ever created.
