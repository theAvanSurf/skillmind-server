using SkillMind.Core.Application.Dtos.Stripe;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;
using SkillMind.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Stripe;
using Stripe.Checkout;
using Stripe.Entitlements;

namespace SkillMind.Infrastructure.Shared.Services;

public class StripeServices(IOptions<StripeConfigurations> configurations, PriceService service, SessionService sessionService)
{
    private readonly PriceService _priceService = service;
    private readonly StripeConfigurations _stripeConfigurations = configurations.Value;
    private readonly SessionService _sessionService = sessionService;

    private async Task<Price> ResolvePriceByLookupKey(string lookupKey)
    {
        var priceOptions = new PriceListOptions
        {
            LookupKeys = new List<string> { lookupKey }
        };

        StripeList<Price>? prices = await _priceService.ListAsync(priceOptions);
        var selectedPrice = prices?.Data?.FirstOrDefault();
        if (selectedPrice is null || string.IsNullOrWhiteSpace(selectedPrice.Id))
            throw new InvalidOperationException($"No Stripe price found for lookup key '{lookupKey}'.");

        return selectedPrice;
    }

    private static string? ExtractClientSecretFromInvoiceJson(string? invoiceJson)
    {
        if (string.IsNullOrWhiteSpace(invoiceJson))
            return null;

        using var document = JsonDocument.Parse(invoiceJson);
        var invoiceRoot = document.RootElement;

        // Preferred path in latest Stripe API shapes.
        if (invoiceRoot.TryGetProperty("confirmation_secret", out var confirmationSecret) &&
            confirmationSecret.TryGetProperty("client_secret", out var confirmationClientSecret) &&
            confirmationClientSecret.ValueKind == JsonValueKind.String)
        {
            return confirmationClientSecret.GetString();
        }

        // Backward-compatible path for expanded payment_intent.
        if (invoiceRoot.TryGetProperty("payment_intent", out var paymentIntent) &&
            paymentIntent.ValueKind == JsonValueKind.Object &&
            paymentIntent.TryGetProperty("client_secret", out var paymentIntentClientSecret) &&
            paymentIntentClientSecret.ValueKind == JsonValueKind.String)
        {
            return paymentIntentClientSecret.GetString();
        }

        return null;
    }

    private static DateTimeOffset? TryReadUnixDate(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return null;

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var unix))
            return null;

        if (unix <= 0)
            return null;

        return DateTimeOffset.FromUnixTimeSeconds(unix);
    }

    private static (string Plan, DateTimeOffset? CurrentPeriodEnd) ExtractPlanAndPeriodEnd(Subscription subscription)
    {
        var plan = "unknown";
        DateTimeOffset? currentPeriodEnd = null;

        if (subscription.Items?.Data != null)
        {
            foreach (var item in subscription.Items.Data)
            {
                if (item.Price != null)
                {
                    if (!string.IsNullOrWhiteSpace(item.Price.LookupKey))
                    {
                        plan = item.Price.LookupKey;
                        break;
                    }

                    if (!string.IsNullOrWhiteSpace(item.Price.Id))
                    {
                        plan = item.Price.Id;
                        break;
                    }
                }
            }
        }

        var rawJson = subscription.StripeResponse?.Content;
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (plan == "unknown")
            {
                if (root.TryGetProperty("items", out var items) &&
                    items.TryGetProperty("data", out var data) &&
                    data.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in data.EnumerateArray())
                    {
                        if (!item.TryGetProperty("price", out var price) || price.ValueKind != JsonValueKind.Object)
                            continue;

                        if (price.TryGetProperty("lookup_key", out var lookupKey) && lookupKey.ValueKind == JsonValueKind.String)
                        {
                            var lookupValue = lookupKey.GetString();
                            if (!string.IsNullOrWhiteSpace(lookupValue))
                            {
                                plan = lookupValue;
                                break;
                            }
                        }

                        if (price.TryGetProperty("id", out var priceId) && priceId.ValueKind == JsonValueKind.String)
                        {
                            var priceValue = priceId.GetString();
                            if (!string.IsNullOrWhiteSpace(priceValue))
                            {
                                plan = priceValue;
                                break;
                            }
                        }
                    }
                }
            }

            if (currentPeriodEnd == null)
            {
                currentPeriodEnd = TryReadUnixDate(root, "current_period_end");
            }
        }

        return (plan, currentPeriodEnd);
    }

    private async Task<string> ExtractClientSecretFromInvoiceId(string invoiceId)
    {
        if (string.IsNullOrWhiteSpace(invoiceId))
            throw new InvalidOperationException("Stripe did not return a latest invoice for this subscription.");

        var invoiceService = new InvoiceService();
        var invoice = await invoiceService.GetAsync(invoiceId, new InvoiceGetOptions
        {
            Expand = new List<string>
            {
                "payment_intent",
                "confirmation_secret",
            },
        });

        var clientSecret = ExtractClientSecretFromInvoiceJson(invoice.StripeResponse?.Content);
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Stripe did not return a payment intent client secret for this subscription.");

        return clientSecret;
    }

    public async Task<string> CreateSession(string lookupKey, string origin)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
            throw new ArgumentException("lookupKey is required.", nameof(lookupKey));

        if (string.IsNullOrWhiteSpace(origin))
            throw new ArgumentException("origin is required.", nameof(origin));

        var selectedPrice = await ResolvePriceByLookupKey(lookupKey);

        var options = new SessionCreateOptions
        {
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = selectedPrice.Id,
                    Quantity = 1,
                },
            },
            Mode = "subscription",
            UiMode = "embedded",
            ReturnUrl = origin + "/return?session_id={CHECKOUT_SESSION_ID}",
        };

        var session = await _sessionService.CreateAsync(options);
        return session.ClientSecret;
    }

    public async Task<(string ClientSecret, string SubscriptionId)> CreateSubscriptionPaymentIntent(string lookupKey, string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(lookupKey))
            throw new ArgumentException("lookupKey is required.", nameof(lookupKey));

        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("customerEmail is required.", nameof(customerEmail));

        var selectedPrice = await ResolvePriceByLookupKey(lookupKey);

        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = customerEmail,
            Limit = 1,
        });

        var customer = customers.Data?.FirstOrDefault();
        if (customer is null)
        {
            customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Email = customerEmail,
            });
        }

        var subscriptionService = new SubscriptionService();

        var existingSubscriptions = await subscriptionService.ListAsync(new SubscriptionListOptions
        {
            Customer = customer.Id,
            Status = "all",
            Limit = 20,
        });

        var activeSubscription = existingSubscriptions.Data?.FirstOrDefault(s =>
            s.Status == "active" || s.Status == "trialing");

        if (activeSubscription is not null)
            throw new InvalidOperationException("An active subscription already exists for this account.");

        var resumableSubscription = existingSubscriptions.Data?.FirstOrDefault(s =>
            (s.Status == "incomplete" || s.Status == "past_due") &&
            !string.IsNullOrWhiteSpace(s.LatestInvoiceId));

        if (resumableSubscription is not null)
        {
            var existingClientSecret = await ExtractClientSecretFromInvoiceId(resumableSubscription.LatestInvoiceId);
            return (existingClientSecret, resumableSubscription.Id);
        }

        var subscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
        {
            Customer = customer.Id,
            Items = new List<SubscriptionItemOptions>
            {
                new()
                {
                    Price = selectedPrice.Id,
                },
            },
            PaymentBehavior = "default_incomplete",
            PaymentSettings = new SubscriptionPaymentSettingsOptions
            {
                SaveDefaultPaymentMethod = "on_subscription",
            },
            Expand = new List<string>
            {
                "latest_invoice.payment_intent",
                "latest_invoice.confirmation_secret",
            },
        });

        var clientSecret = await ExtractClientSecretFromInvoiceId(subscription.LatestInvoiceId);

        return (clientSecret, subscription.Id);
    }

    public async Task<SubscriptionDto?> GetSubscription(string customerEmail, string userId)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            throw new ArgumentException("customerEmail is required.", nameof(customerEmail));

        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = customerEmail,
            Limit = 1,
        });

        var customer = customers.Data?.FirstOrDefault();
        if (customer is null)
            return null;

        var subscriptionService = new SubscriptionService();
        var subscriptions = await subscriptionService.ListAsync(new SubscriptionListOptions
        {
            Customer = customer.Id,
            Status = "all",
            Limit = 20,
        });

        var selected = subscriptions.Data?.FirstOrDefault(s => s.Status == "active" || s.Status == "trialing")
            ?? subscriptions.Data?.FirstOrDefault(s => s.Status == "incomplete" || s.Status == "past_due")
            ?? subscriptions.Data?.FirstOrDefault();

        if (selected is null)
            return null;

        var (plan, currentPeriodEnd) = ExtractPlanAndPeriodEnd(selected);
        var isInGracePeriod = selected.Status == "past_due";

        return new SubscriptionDto
        {
            Id = selected.Id,
            UserId = userId,
            StripeSubscriptionId = selected.Id,
            Plan = plan,
            SubscriptionStatus = selected.Status,
            CurrentPeriodEnd = (currentPeriodEnd ?? DateTimeOffset.UtcNow).ToString("O"),
            IsInGracePeriod = isInGracePeriod,
            GracePeriodEnd = isInGracePeriod ? (currentPeriodEnd ?? DateTimeOffset.UtcNow).ToString("O") : null,
            IntendedPlan = selected.Status == "incomplete" || selected.Status == "past_due" ? plan : null,
        };
    }

    public async Task<SessionStatusDto> GetSessionStatus(string sessionId)
    {
        var session = await _sessionService.GetAsync(sessionId);
        return new SessionStatusDto
        {
            Status = session.Status,
            CustomerEmail = session.CustomerDetails.Email
        };
    }

    public async Task<string> CreatePortalSession(string sessionId, string origin)
    {
        var checkoutSession = await _sessionService.GetAsync(sessionId);

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = checkoutSession.CustomerId,
            ReturnUrl = origin,
        };

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreatePortalSessionByCustomerEmail(string customerEmail, string origin)
    {
        var customerService = new CustomerService();
        var customers = await customerService.ListAsync(new CustomerListOptions
        {
            Email = customerEmail,
            Limit = 1,
        });

        var customer = customers.Data?.FirstOrDefault();
        if (customer is null)
            throw new InvalidOperationException("No Stripe customer found for this account.");

        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = customer.Id,
            ReturnUrl = origin,
        };

        var portalService = new Stripe.BillingPortal.SessionService();
        var session = await portalService.CreateAsync(options);
        return session.Url;
    }

    public async Task<List<PaymentIntent>> GetSucceededCoursePurchaseIntentsAsync()
    {
        var service = new PaymentIntentService();
        var result = new List<PaymentIntent>();
        var options = new PaymentIntentListOptions { Limit = 100 };

        await foreach (var intent in service.ListAutoPagingAsync(options))
        {
            if (intent.Status == "succeeded" &&
                intent.Metadata.TryGetValue("type", out var type) &&
                type == "course_purchase")
            {
                result.Add(intent);
            }
        }

        return result;
    }

    public Task<bool> HandleWebhook(string json, string stripeSignature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                stripeSignature,
                _stripeConfigurations.WebhookSecret,
                throwOnApiVersionMismatch: false
            );

            switch (StripeEventMapper.Map(stripeEvent.Type))
            {
                case StripeEventType.CustomerSubscriptionCreated:
                    var createdSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionUpdated:
                    var updatedSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionDeleted:
                    var deletedSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.CustomerSubscriptionTrialWillEnd:
                    var trialSub = stripeEvent.Data.Object as Subscription;
                    break;

                case StripeEventType.ActiveEntitlementSummaryUpdated:
                    var summary = stripeEvent.Data.Object as ActiveEntitlementSummary;
                    break;

                case StripeEventType.Unknown:
                default:
                    Console.WriteLine("Unhandled Stripe event: {0}", stripeEvent.Type);
                    break;
            }

            return Task.FromResult(true);
        }
        catch (StripeException e)
        {
            Console.WriteLine("Stripe webhook error: {0}", e.Message);
            return Task.FromResult(false);
        }
    }

    // ── Stripe Connect ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates (or reuses) an Express connected account and returns an AccountLink onboarding URL.
    /// </summary>
    public async Task<(string AccountId, string OnboardingUrl)> CreateConnectAccountAsync(
        string? existingAccountId, string returnUrl, string refreshUrl)
    {
        string accountId;

        if (string.IsNullOrWhiteSpace(existingAccountId))
        {
            var accountOptions = new AccountCreateOptions
            {
                Type = "express",
                Capabilities = new AccountCapabilitiesOptions
                {
                    CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                    Transfers = new AccountCapabilitiesTransfersOptions { Requested = true }
                }
            };
            var accountService = new AccountService();
            var account = await accountService.CreateAsync(accountOptions);
            accountId = account.Id;
        }
        else
        {
            accountId = existingAccountId;
        }

        var linkOptions = new AccountLinkCreateOptions
        {
            Account = accountId,
            RefreshUrl = refreshUrl,
            ReturnUrl = returnUrl,
            Type = "account_onboarding"
        };
        var linkService = new AccountLinkService();
        var link = await linkService.CreateAsync(linkOptions);

        return (accountId, link.Url);
    }

    /// <summary>
    /// Retrieves the live charges_enabled / payouts_enabled status for a connected account.
    /// </summary>
    public async Task<(bool ChargesEnabled, bool PayoutsEnabled)> GetConnectAccountStatusAsync(string accountId)
    {
        var accountService = new AccountService();
        var account = await accountService.GetAsync(accountId);
        return (account.ChargesEnabled, account.PayoutsEnabled);
    }

    /// <summary>
    /// Returns the pending balance (in the account's default currency) for a connected account.
    /// </summary>
    public async Task<decimal> GetConnectPendingBalanceAsync(string accountId)
    {
        var balanceService = new BalanceService();
        var balance = await balanceService.GetAsync(new RequestOptions { StripeAccount = accountId });
        var pending = balance.Pending?.FirstOrDefault();
        return pending is null ? 0m : pending.Amount / 100m;
    }

    /// <summary>
    /// Creates a PaymentIntent for a course purchase, routing funds to the professor's connected account.
    /// </summary>
    public async Task<(string ClientSecret, string PaymentIntentId)> CreateCoursePurchaseIntentAsync(
        decimal coursePrice, string connectedAccountId, string courseId, string studentProfileId)
    {
        var amountCents = (long)Math.Round(coursePrice * 100);
        var applicationFeeCents = (long)Math.Round(amountCents * (double)_stripeConfigurations.PlatformFeePercent);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountCents,
            Currency = "usd",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never",
            },
            ApplicationFeeAmount = applicationFeeCents,
            TransferData = new PaymentIntentTransferDataOptions
            {
                Destination = connectedAccountId
            },
            Metadata = new Dictionary<string, string>
            {
                ["courseId"] = courseId,
                ["studentProfileId"] = studentProfileId,
                ["type"] = "course_purchase"
            }
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options);
        return (intent.ClientSecret, intent.Id);
    }

    /// <summary>
    /// Validates a Connect webhook signature and returns the event.
    /// </summary>
    public Event ConstructConnectEvent(string json, string stripeSignature)
    {
        var secret = !string.IsNullOrWhiteSpace(_stripeConfigurations.ConnectWebhookSecret)
            ? _stripeConfigurations.ConnectWebhookSecret
            : _stripeConfigurations.WebhookSecret;

        return EventUtility.ConstructEvent(json, stripeSignature, secret, throwOnApiVersionMismatch: false);
    }
}