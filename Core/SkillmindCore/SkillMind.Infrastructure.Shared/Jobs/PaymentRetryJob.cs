using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Settings;

namespace SkillMind.Infrastructure.Shared.Jobs;

/// <summary>
/// Runs every <see cref="StripeConfigurations.RetryIntervalHours"/> hours.
/// Finds subscriptions due for a payment retry and delegates to <see cref="IStripeServices"/>.
/// </summary>
public class PaymentRetryJob(
    IServiceScopeFactory scopeFactory,
    IOptions<StripeConfigurations> stripeOptions,
    ILogger<PaymentRetryJob> logger)
    : BackgroundService
{
    private readonly StripeConfigurations _cfg = stripeOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_cfg.RetryIntervalHours);
        logger.LogInformation("{Job} started. Runs every {Interval}.", nameof(PaymentRetryJob), interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{Job} encountered an error.", nameof(PaymentRetryJob));
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var stripeService = scope.ServiceProvider.GetRequiredService<IStripeServices>();

        var due = await subscriptionRepo.GetDueForRetry(ct);
        var list = due.ToList();

        if (list.Count == 0) return;

        logger.LogInformation("{Job}: Found {Count} subscription(s) due for retry.", nameof(PaymentRetryJob), list.Count);

        foreach (var sub in list)
        {
            if (sub.RetryAttemptCount >= _cfg.MaxRetryAttempts)
            {
                logger.LogInformation("{Job}: Skipping {SubId} — max retries ({Max}) reached.",
                    nameof(PaymentRetryJob), sub.StripeSubscriptionId, _cfg.MaxRetryAttempts);
                continue;
            }

            logger.LogInformation("{Job}: Retrying payment for {SubId} (attempt {Attempt}/{Max}).",
                nameof(PaymentRetryJob), sub.StripeSubscriptionId, sub.RetryAttemptCount + 1, _cfg.MaxRetryAttempts);

            await stripeService.RetryFailedPayment(sub.StripeSubscriptionId, ct);
        }
    }
}
