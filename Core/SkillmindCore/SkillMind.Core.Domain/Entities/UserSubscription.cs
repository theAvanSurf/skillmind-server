using SkillMind.Core.Domain.Enums;

namespace SkillMind.Core.Domain.Entities;

/// <summary>
/// Tracks a user's Stripe subscription state, grace period, and retry metadata.
/// </summary>
public class UserSubscription
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripePriceId { get; set; }
    public string? StripeLookupKey { get; set; }

    /// <summary>Raw Stripe status string (e.g., "active", "past_due").</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Typed subscription status used for business-logic branching.</summary>
    public SubscriptionStatus SubscriptionStatus { get; set; }

    public string Plan { get; set; } = string.Empty;
    public DateTime CurrentPeriodStart { get; set; }
    public DateTime CurrentPeriodEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? TrialEnd { get; set; }

    // ── Grace period ────────────────────────────────────────────────────────
    /// <summary>UTC timestamp when the grace period began (first payment failure).</summary>
    public DateTime? GracePeriodStart { get; set; }

    /// <summary>UTC timestamp when the grace period expires (GracePeriodStart + GracePeriodDays).</summary>
    public DateTime? GracePeriodEnd { get; set; }

    /// <summary>True while the subscription is within its grace window.</summary>
    public bool IsInGracePeriod { get; set; }

    // ── Retry tracking ───────────────────────────────────────────────────────
    /// <summary>Number of automatic payment retry attempts made so far.</summary>
    public int RetryAttemptCount { get; set; }

    /// <summary>UTC timestamp for the next scheduled retry (+RetryIntervalHours per attempt).</summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>UTC timestamp of the most recent payment failure.</summary>
    public DateTime? LastFailureAt { get; set; }

    /// <summary>Machine-readable reason for the last payment failure.</summary>
    public PaymentFailureReason? LastFailureReason { get; set; }

    /// <summary>Idempotency key used on the last retry to prevent duplicate charges.</summary>
    public string? LastIdempotencyKey { get; set; }

    /// <summary>
    /// Set when a user starts a paid checkout but hasn't completed payment yet.
    /// Cleared once the subscription is confirmed via webhook.
    /// Used on login to redirect the user back to complete their purchase.
    /// </summary>
    public string? IntendedPlan { get; set; }

    // ── Audit ────────────────────────────────────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>EF Core row-version token for optimistic concurrency.</summary>
    public uint RowVersion { get; set; }
}
