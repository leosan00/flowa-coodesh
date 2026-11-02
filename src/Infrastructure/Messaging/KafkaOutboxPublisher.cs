using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class KafkaOutboxPublisher(IProducer<string, string> producer, IOptions<KafkaOptions> options) : IOutboxPublisher
{
    public async Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var kafkaMessage = new Message<string, string>
        {
            Key = message.Key,
            Value = message.Payload,
            Headers = new Headers
            {
                { "event-id", Encoding.UTF8.GetBytes(message.Id.ToString()) },
                { "event-type", Encoding.UTF8.GetBytes(message.Type) }
            }
        };

        await producer.ProduceAsync(options.Value.Topic, kafkaMessage, cancellationToken);
    }
}
