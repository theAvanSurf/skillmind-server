using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>Published when an invoice payment fails.</summary>
public record PaymentFailedMessage(
    Guid EventId,
    DateTime OccurredAt,
    Guid UserId,
    string StripeSubscriptionId,
    string CustomerEmail,
    PaymentFailureReason FailureReason,
    DateTime GracePeriodEnd,
    int RetryAttemptCount
);
