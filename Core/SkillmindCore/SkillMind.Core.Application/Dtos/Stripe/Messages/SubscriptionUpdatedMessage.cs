namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a subscription status or plan changes.</summary>
public record SubscriptionUpdatedMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    string NewStatus,
    string Plan
);
