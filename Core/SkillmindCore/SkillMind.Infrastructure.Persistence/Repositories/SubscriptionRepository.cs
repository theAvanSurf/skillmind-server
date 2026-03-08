using Microsoft.EntityFrameworkCore;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Entities;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ISubscriptionRepository"/>.
/// </summary>
public class SubscriptionRepository(SkillMindDbContext context) : ISubscriptionRepository
{
    public async Task<UserSubscription?> GetByStripeSubscriptionId(string stripeSubscriptionId, CancellationToken ct = default)
        => await context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscriptionId, ct);

    public async Task<UserSubscription?> GetByUserId(Guid userId, CancellationToken ct = default)
        => await context.UserSubscriptions
            .FirstOrDefaultAsync(s => s.UserId == userId, ct);

    /// <summary>
    /// Returns subscriptions that are inside a grace period whose deadline has passed.
    /// Called by the grace-period enforcement background job.
    /// </summary>
    public async Task<IEnumerable<UserSubscription>> GetExpiredGracePeriods(CancellationToken ct = default)
        => await context.UserSubscriptions
            .Where(s => s.IsInGracePeriod && s.GracePeriodEnd.HasValue && s.GracePeriodEnd.Value <= DateTime.UtcNow)
            .ToListAsync(ct);

    /// <summary>
    /// Returns subscriptions that are still within their grace period, have failed attempts,
    /// and whose <see cref="UserSubscription.NextRetryAt"/> has arrived.
    /// </summary>
    public async Task<IEnumerable<UserSubscription>> GetDueForRetry(CancellationToken ct = default)
        => await context.UserSubscriptions
            .Where(s =>
                s.IsInGracePeriod &&
                s.GracePeriodEnd.HasValue &&
                s.GracePeriodEnd.Value > DateTime.UtcNow &&
                s.NextRetryAt.HasValue &&
                s.NextRetryAt.Value <= DateTime.UtcNow)
            .ToListAsync(ct);

    public async Task CreateAsync(UserSubscription subscription, CancellationToken ct = default)
    {
        await context.UserSubscriptions.AddAsync(subscription, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(UserSubscription subscription, CancellationToken ct = default)
    {
        context.UserSubscriptions.Update(subscription);
        await context.SaveChangesAsync(ct);
    }
}
