using SkillMind.Core.Application.Dtos.Common;

namespace SkillMind.Application.Interfaces;

public interface IKafkaEventService
{
    Task PublishAsync(string topic, object payload, CancellationToken ct = default);
    Task ConsumeAsync<T>(
        IReadOnlyCollection<string> topics,
        string consumerGroup,
        Func<string, T?, Task> handler,
        CancellationToken ct = default);
    Task<List<string>> GetTopicsAsync();
    Task<KafkaTopicInfo?> GetTopicMetadataAsync(string topic);
}