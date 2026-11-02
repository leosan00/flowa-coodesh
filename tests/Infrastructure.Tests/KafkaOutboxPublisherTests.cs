using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Moq;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Tests;

public class KafkaOutboxPublisherTests
{
    private readonly Mock<IProducer<string, string>> _producer = new();

    private KafkaOutboxPublisher CreatePublisher(string topic = "orders") =>
        new(_producer.Object, Options.Create(new KafkaOptions { Topic = topic }));

    private static OutboxMessage MessageOf() => new()
    {
        Id = Guid.NewGuid(),
        Type = "OrderRejected",
        Key = "VALE3",
        Payload = "{\"symbol\":\"VALE3\"}",
        OccurredAtUtc = DateTime.UtcNow
    };

    [Fact]
    public async Task Sends_the_payload_to_the_topic_keyed_by_symbol_with_event_headers()
    {
        string? topic = null;
        Message<string, string>? sent = null;
        _producer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .Callback((string t, Message<string, string> m, CancellationToken _) => (topic, sent) = (t, m))
            .ReturnsAsync(new DeliveryResult<string, string>());
        var message = MessageOf();

        await CreatePublisher("pedidos").PublishAsync(message);

        Assert.Equal("pedidos", topic);
        Assert.NotNull(sent);
        Assert.Equal("VALE3", sent.Key);
        Assert.Equal("{\"symbol\":\"VALE3\"}", sent.Value);
        Assert.Equal(message.Id.ToString(), Encoding.UTF8.GetString(sent.Headers.GetLastBytes("event-id")));
        Assert.Equal("OrderRejected", Encoding.UTF8.GetString(sent.Headers.GetLastBytes("event-type")));
    }

    [Fact]
    public async Task Delivery_failure_is_not_swallowed()
    {
        _producer
            .Setup(p => p.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProduceException<string, string>(new Error(ErrorCode.Local_MsgTimedOut), new DeliveryResult<string, string>()));

        await Assert.ThrowsAsync<ProduceException<string, string>>(() => CreatePublisher().PublishAsync(MessageOf()));
    }
}
