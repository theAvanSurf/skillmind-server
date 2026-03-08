using SkillMind.Core.Domain.Entities;

namespace SkillMind.Core.Application.Interfaces;

/// <summary>Data-access contract for <see cref="UserSubscription"/> records.</summary>
public interface ISubscriptionRepository
{
    /// <summary>Returns the subscription matching the Stripe subscription ID, or null.</summary>
    Task<UserSubscription?> GetByStripeSubscriptionId(string subscriptionId, CancellationToken ct = default);

    /// <summary>Returns the subscription belonging to the given user, or null.</summary>
    Task<UserSubscription?> GetByUserId(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns all subscriptions where <c>IsInGracePeriod = true</c>
    /// and <c>GracePeriodEnd &lt;= UtcNow</c>.
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetExpiredGracePeriods(CancellationToken ct = default);

    /// <summary>
    /// Returns subscriptions where <c>NextRetryAt &lt;= UtcNow</c>
    /// and <c>RetryAttemptCount &lt; MaxRetryAttempts</c>
    /// and the grace period has not expired.
    /// </summary>
    Task<IEnumerable<UserSubscription>> GetDueForRetry(CancellationToken ct = default);

    /// <summary>Persists changes to an existing subscription (optimistic concurrency safe).</summary>
    Task UpdateAsync(UserSubscription subscription, CancellationToken ct = default);

    /// <summary>Persists a new subscription record.</summary>
    Task CreateAsync(UserSubscription subscription, CancellationToken ct = default);
}
