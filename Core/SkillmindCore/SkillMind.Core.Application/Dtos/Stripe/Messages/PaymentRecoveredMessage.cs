namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when a previously failed payment is collected successfully.</summary>
public record PaymentRecoveredMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    DateTime RecoveredAt
);
