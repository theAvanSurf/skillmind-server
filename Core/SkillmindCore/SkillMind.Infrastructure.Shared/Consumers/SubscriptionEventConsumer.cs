using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Stripe.Messages;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Infrastructure.Shared.Consumers;

/// <summary>
/// Listens to all Stripe subscription lifecycle topics and routes email notifications.
/// </summary>
public class SubscriptionEventConsumer(
    IKafkaEventService kafka,
    IServiceScopeFactory scopeFactory,
    ILogger<SubscriptionEventConsumer> logger)
    : BackgroundService
{
    private static readonly IReadOnlyCollection<string> Topics =
    [
        StripeKafkaTopic.SubscriptionCreated.ToTopicName(),
        StripeKafkaTopic.SubscriptionUpdated.ToTopicName(),
        StripeKafkaTopic.SubscriptionCanceled.ToTopicName(),
        StripeKafkaTopic.SubscriptionTrialEnding.ToTopicName(),
    ];

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Consumer} started, listening to: {Topics}",
            nameof(SubscriptionEventConsumer), string.Join(", ", Topics));

        // Canceled topic
        var canceledTask = kafka.ConsumeAsync<SubscriptionCanceledMessage>(
            [StripeKafkaTopic.SubscriptionCanceled.ToTopicName()],
            "skillmind-subscription-canceled-email",
            async (_, msg) =>
            {
                if (msg is null) return;
                await SendEmail(async email => await email.SendSubscriptionCanceledEmail(
                    msg.CustomerEmail, "Subscriber", stoppingToken));
            },
            stoppingToken);

        // Trial ending topic
        var trialTask = kafka.ConsumeAsync<SubscriptionTrialEndingMessage>(
            [StripeKafkaTopic.SubscriptionTrialEnding.ToTopicName()],
            "skillmind-trial-ending-email",
            async (_, msg) =>
            {
                if (msg is null) return;
                await SendEmail(async email => await email.SendTrialEndingEmail(
                    msg.CustomerEmail, "Subscriber", msg.TrialEnd, stoppingToken));
            },
            stoppingToken);

        return Task.WhenAll(canceledTask, trialTask);
    }

    private async Task SendEmail(Func<IEmailNotificationService, Task> action)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailNotificationService>();
        try
        {
            await action(emailService);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Email send failed in {Consumer}", nameof(SubscriptionEventConsumer));
        }
    }
}
