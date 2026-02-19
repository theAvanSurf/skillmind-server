namespace SkillMind.Core.Application.Dtos.Common;

public record KafkaTopicInfo(
    string Topic,
    int PartitionCount,
    IReadOnlyList<int> PartitionIds
);