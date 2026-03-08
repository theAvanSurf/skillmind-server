using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Stripe.Messages;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;
using SkillMind.Core.Domain.Settings;

namespace SkillMind.Infrastructure.Shared.Jobs;

/// <summary>
/// Runs every hour. Finds subscriptions whose grace period has expired and suspends them.
/// Publishes <see cref="SubscriptionSuspendedMessage"/> to Kafka and sends an email.
/// </summary>
public class GracePeriodEnforcementJob(
    IServiceScopeFactory scopeFactory,
    IKafkaEventService kafka,
    ILogger<GracePeriodEnforcementJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Job} started. Runs every {Interval}.", nameof(GracePeriodEnforcementJob), Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Job} encountered an error.", nameof(GracePeriodEnforcementJob));
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();

        var expired = await subscriptionRepo.GetExpiredGracePeriods(ct);
        var list = expired.ToList();

        if (list.Count == 0) return;

        logger.LogInformation("{Job}: Found {Count} expired grace period(s) to suspend.", nameof(GracePeriodEnforcementJob), list.Count);

        foreach (var sub in list)
        {
            sub.SubscriptionStatus = SubscriptionStatus.Suspended;
            sub.Status = "suspended";
            sub.IsInGracePeriod = false;
            sub.UpdatedAt = DateTime.UtcNow;

            await subscriptionRepo.UpdateAsync(sub, ct);

            // Email notification
            await emailService.SendSubscriptionSuspendedEmail(string.Empty, "Subscriber", ct);

            // Kafka event
            await kafka.PublishAsync(
                StripeKafkaTopic.SubscriptionSuspended.ToTopicName(),
                new SubscriptionSuspendedMessage(
                    Guid.NewGuid(), DateTime.UtcNow,
                    sub.UserId, sub.StripeSubscriptionId,
                    string.Empty, DateTime.UtcNow),
                ct);

            logger.LogInformation("{Job}: Suspended subscription {SubId}.", nameof(GracePeriodEnforcementJob), sub.StripeSubscriptionId);
        }
    }
}
