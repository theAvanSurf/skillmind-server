namespace SkillMind.Core.Application.Dtos.Stripe.Messages;

/// <summary>
/// Dead Letter Queue message — wraps any event that could not be processed.
/// Never contains card data, CVV, or raw Stripe tokens.
/// </summary>
public record FailedEventDLQMessage(
    Guid EventId,
    DateTime OccurredAt,
    string OriginalTopic,
    string OriginalPayload,
    string Error,
    DateTime FailedAt
);
