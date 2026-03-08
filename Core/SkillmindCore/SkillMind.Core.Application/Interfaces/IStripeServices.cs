using SkillMind.Core.Application.Dtos.Stripe;

namespace SkillMind.Core.Application.Interfaces;

/// <summary>Exposes all Stripe operations used by the application layer and controllers.</summary>
public interface IStripeServices
{
    /// <summary>Creates a Stripe Subscription using the Elements flow and returns the PaymentIntent client secret.</summary>
    /// <param name="userEmail">Used to create/retrieve the Stripe Customer.</param>
    Task<SubscriptionClientSecretDto> CreateSubscription(string lookupKey, Guid userId, string userEmail, CancellationToken ct = default);

    /// <summary>Creates a Stripe Checkout Session and returns the client secret.</summary>
    /// <param name="userId">The authenticated user's ID — stored in Stripe subscription metadata to link the record on webhook receipt.</param>
    Task<string> CreateSession(string lookupKey, string origin, Guid userId, CancellationToken ct = default);

    /// <summary>Returns the status and customer email of an existing Checkout Session.</summary>
    Task<SessionStatusDto> GetSessionStatus(string sessionId, CancellationToken ct = default);

    /// <summary>Creates a Billing Portal session and returns the redirect URL.</summary>
    Task<string> CreatePortalSession(string sessionId, string origin, CancellationToken ct = default);

    /// <summary>
    /// Validates the Stripe webhook signature and processes the event.
    /// Returns <c>false</c> if the signature is invalid; never swallows exceptions silently.
    /// </summary>
    Task<bool> HandleWebhook(string json, string signature, CancellationToken ct = default);

    /// <summary>
    /// Retries the latest unpaid invoice for the given subscription.
    /// Generates an idempotency key automatically and persists it to the DB.
    /// Publishes <see cref="Dtos.Stripe.Messages.PaymentRecoveredMessage"/> on success
    /// or <see cref="Dtos.Stripe.Messages.PaymentFailedMessage"/> on failure.
    /// </summary>
    Task RetryFailedPayment(string stripeSubscriptionId, CancellationToken ct = default);
}
