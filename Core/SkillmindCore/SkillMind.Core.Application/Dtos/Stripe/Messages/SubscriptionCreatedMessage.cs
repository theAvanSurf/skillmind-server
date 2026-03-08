namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a new subscription is created via CustomerSubscriptionCreated.</summary>
public record SubscriptionCreatedMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    string Plan,
    DateTime CurrentPeriodEnd
);
