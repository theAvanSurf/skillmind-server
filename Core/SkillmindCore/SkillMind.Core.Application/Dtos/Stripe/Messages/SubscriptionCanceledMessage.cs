namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a subscription is deleted/canceled.</summary>
public record SubscriptionCanceledMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    DateTime CanceledAt
);
