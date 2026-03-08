namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a subscription is suspended after the grace period expires.</summary>
public record SubscriptionSuspendedMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    DateTime SuspendedAt
);
