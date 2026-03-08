namespace SkillMind.Core.Domain.Entities;

/// <summary>
/// Persisted record of every Kafka event that could not be processed.
/// Consumed by the DLQ consumer and used for manual reconciliation.
/// </summary>
public class FailedStripeEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>The original topic the event was published to.</summary>
    public string OriginalTopic { get; set; } = string.Empty;

    /// <summary>JSON-serialised payload of the original event (no card data).</summary>
    public string OriginalPayload { get; set; } = string.Empty;

    /// <summary>Exception message or error description (no secrets).</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the failure occurred.</summary>
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Set to true once an operator has reviewed and resolved the event.</summary>
    public bool IsResolved { get; set; }
}
