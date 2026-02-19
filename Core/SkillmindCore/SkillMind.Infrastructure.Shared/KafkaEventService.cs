using Confluent.Kafka;
using System.Text.Json;

namespace SkillMind.Infrastructure.Shared;

public class KafkaEventService
{
    private readonly IProducer<string, string> _producer;

    public KafkaEventService(string brokerAddress)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = brokerAddress
        };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync(string topic, object payload)
    {
        var message = JsonSerializer.Serialize(payload);
        await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = message
        });
    }
}