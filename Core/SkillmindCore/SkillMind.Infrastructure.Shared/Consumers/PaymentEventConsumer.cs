using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Stripe.Messages;
using SkillMind.Core.Application.Interfaces;
using SkillMind.Core.Domain.Enums;

namespace SkillMind.Infrastructure.Shared.Consumers;

/// <summary>
/// Listens to payment failed / recovered Kafka topics and sends email notifications.
/// </summary>
public class PaymentEventConsumer(
    IKafkaEventService kafka,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentEventConsumer> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Consumer} started.", nameof(PaymentEventConsumer));

        var failedTask = kafka.ConsumeAsync<PaymentFailedMessage>(
            [StripeKafkaTopic.PaymentFailed.ToTopicName()],
            "skillmind-payment-failed-email",
            async (_, msg) =>
            {
                if (msg is null) return;
                await SendEmail(async email => await email.SendPaymentFailedEmail(
                    msg.CustomerEmail, "Subscriber", msg.GracePeriodEnd, stoppingToken));
            },
            stoppingToken);

        var recoveredTask = kafka.ConsumeAsync<PaymentRecoveredMessage>(
            [StripeKafkaTopic.PaymentRecovered.ToTopicName()],
            "skillmind-payment-recovered-email",
            async (_, msg) =>
            {
                if (msg is null) return;
                await SendEmail(async email => await email.SendPaymentRecoveredEmail(
                    msg.CustomerEmail, "Subscriber", stoppingToken));
            },
            stoppingToken);

        return Task.WhenAll(failedTask, recoveredTask);
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
            logger.LogError(ex, "Email send failed in {Consumer}", nameof(PaymentEventConsumer));
        }
    }
}
