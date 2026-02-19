using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SkillMind.Application.Interfaces;
using SkillMind.Core.Application.Dtos.Common;
using SkillMind.Core.Domain.Settings;

namespace SkillMind.Infrastructure.Shared;

public sealed class KafkaEventService(
    IProducer<string, string> producer,
    IAdminClient adminClient,
    IOptions<KafkaSettings> settings)
    : IKafkaEventService, IDisposable
{
    private readonly KafkaSettings _settings = settings.Value;
    private bool _disposed;

    public async Task PublishAsync(string topic, object payload, CancellationToken ct = default)
    {
        var message = JsonSerializer.Serialize(payload);
        try
        {
            var result = await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message
            }, ct);

            Console.WriteLine($"[Kafka] Delivered to {result.TopicPartitionOffset}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"[Kafka] Failed to deliver: {ex.Error.Reason}");
            throw;
        }
    }

    public Task ConsumeAsync<T>(
        IReadOnlyCollection<string> topics,
        string consumerGroup,
        Func<string, T?, Task> handler,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
            {
                BootstrapServers = _settings.BrokerAddress,
                GroupId = consumerGroup,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            })
            .SetErrorHandler((_, e) => Console.WriteLine($"[Kafka Consumer Error] {e.Reason}"))
            .SetPartitionsAssignedHandler((_, partitions) =>
                Console.WriteLine($"[Kafka] Assigned: {string.Join(", ", partitions)}"))
            .SetPartitionsRevokedHandler((_, partitions) =>
                Console.WriteLine($"[Kafka] Revoked: {string.Join(", ", partitions)}"))
            .Build();

            consumer.Subscribe(topics);

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var consumeResult = consumer.Consume(ct);
                        if (consumeResult?.Message is null) continue;

                        var deserialized = JsonSerializer.Deserialize<T>(consumeResult.Message.Value);
                        await handler(consumeResult.Topic, deserialized);
                        consumer.Commit(consumeResult);
                    }
                    catch (ConsumeException ex)
                    {
                        Console.WriteLine($"[Kafka] Consume error: {ex.Error.Reason}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Kafka] Handler error: {ex.Message}");
                    }
                }
            }
            finally
            {
                consumer.Close();
            }
        }, ct);
    }

    public Task<List<string>> GetTopicsAsync()
    {
        var meta = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        return Task.FromResult(meta.Topics.Select(t => t.Topic).ToList());
    }

    public Task<KafkaTopicInfo?> GetTopicMetadataAsync(string topic)
    {
        var meta = adminClient.GetMetadata(topic, TimeSpan.FromSeconds(10));
        var topicMeta = meta.Topics.FirstOrDefault(t => t.Topic == topic);

        if (topicMeta is null)
            return Task.FromResult<KafkaTopicInfo?>(null);

        var info = new KafkaTopicInfo(
            topicMeta.Topic,
            topicMeta.Partitions.Count,
            topicMeta.Partitions.Select(p => p.PartitionId).ToList()
        );

        return Task.FromResult<KafkaTopicInfo?>(info);
    }

    public void Dispose()
    {
        if (_disposed) return;
        producer.Flush(TimeSpan.FromSeconds(10));
        producer.Dispose();
        adminClient.Dispose();
        _disposed = true;
    }
}