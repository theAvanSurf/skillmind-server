namespace SkillMind.Core.Domain.Settings;

public class KafkaSettings
{
    public const string SectionName = "Kafka";
    public string BrokerAddress { get; init; } = string.Empty;
}