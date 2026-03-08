using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Stripe.Messages;
using SkillMind.Core.Domain.Entities;
using SkillMind.Core.Domain.Enums;
using SkillMind.Infrastructure.Persistence.Context;

namespace SkillMind.Infrastructure.Shared.Consumers;

/// <summary>
/// Reads from the Stripe dead-letter queue topic and persists failed events to
/// <see cref="SkillMindDbContext.FailedStripeEvents"/> for manual reprocessing or alerting.
/// </summary>
public class FailedEventConsumer(
    IKafkaEventService kafka,
    IServiceScopeFactory scopeFactory,
    ILogger<FailedEventConsumer> logger)
    : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("{Consumer} started, listening to DLQ: {Topic}",
            nameof(FailedEventConsumer), StripeKafkaTopic.FailedEventDLQ.ToTopicName());

        return kafka.ConsumeAsync<FailedEventDLQMessage>(
            [StripeKafkaTopic.FailedEventDLQ.ToTopicName()],
            "skillmind-failed-event-store",
            async (_, msg) =>
            {
                if (msg is null) return;

                await using var scope = scopeFactory.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<SkillMindDbContext>();

                var entity = new FailedStripeEvent
                {
                    Id = Guid.NewGuid(),
                    OriginalTopic = msg.OriginalTopic,
                    OriginalPayload = msg.OriginalPayload,
                    Error = msg.Error,
                    FailedAt = msg.FailedAt,
                    IsResolved = false,
                };

                db.FailedStripeEvents.Add(entity);

                try
                {
                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogWarning("[DLQ] Stored failed event from topic {Topic}", msg.OriginalTopic);
                }
                catch (DbUpdateException ex)
                {
                    logger.LogError(ex, "[DLQ] Failed to persist event from topic {Topic}", msg.OriginalTopic);
                }
            },
            stoppingToken);
    }
}
