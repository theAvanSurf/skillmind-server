namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a trial period is about to end.</summary>
public record SubscriptionTrialEndingMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    DateTime TrialEnd
);
