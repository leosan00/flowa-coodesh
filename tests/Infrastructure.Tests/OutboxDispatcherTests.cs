using Moq;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Tests;

public class OutboxDispatcherTests
{
    private readonly Mock<IOutboxStore> _store = new();
    private readonly Mock<IOutboxPublisher> _publisher = new();

    private OutboxDispatcher CreateDispatcher() => new(_store.Object, _publisher.Object);

    private static OutboxMessage MessageOf(string key) => new()
    {
        Id = Guid.NewGuid(),
        Type = "OrderAccepted",
        Key = key,
        Payload = "{}",
        OccurredAtUtc = DateTime.UtcNow
    };

    private void StorePending(params OutboxMessage[] messages) =>
        _store
            .Setup(s => s.GetPendingAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

    [Fact]
    public async Task Publishes_pending_messages_in_order_and_marks_each_as_processed()
    {
        var first = MessageOf("PETR4");
        var second = MessageOf("VALE3");
        StorePending(first, second);

        var sequence = new List<string>();
        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Callback((OutboxMessage m, CancellationToken _) => sequence.Add($"publish:{m.Id}"))
            .Returns(Task.CompletedTask);
        _store
            .Setup(s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Callback((Guid id, CancellationToken _) => sequence.Add($"mark:{id}"))
            .Returns(Task.CompletedTask);

        var published = await CreateDispatcher().DispatchPendingAsync(100);

        Assert.Equal(2, published);
        Assert.Equal(
            [$"publish:{first.Id}", $"mark:{first.Id}", $"publish:{second.Id}", $"mark:{second.Id}"],
            sequence);
    }

    [Fact]
    public async Task Does_nothing_when_there_are_no_pending_messages()
    {
        StorePending();

        var published = await CreateDispatcher().DispatchPendingAsync(100);

        Assert.Equal(0, published);
        _publisher.Verify(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(s => s.MarkProcessedAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Asks_the_store_for_the_requested_batch_size()
    {
        StorePending();

        await CreateDispatcher().DispatchPendingAsync(25);

        _store.Verify(s => s.GetPendingAsync(25, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Stops_at_the_first_failure_keeping_the_order_and_what_was_already_published()
    {
        var first = MessageOf("PETR4");
        var failing = MessageOf("PETR4");
        var third = MessageOf("PETR4");
        StorePending(first, failing, third);

        _publisher
            .Setup(p => p.PublishAsync(It.IsAny<OutboxMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _publisher
            .Setup(p => p.PublishAsync(failing, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Kafka indisponível"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateDispatcher().DispatchPendingAsync(100));

        _store.Verify(s => s.MarkProcessedAsync(first.Id, It.IsAny<CancellationToken>()), Times.Once);
        _store.Verify(s => s.MarkProcessedAsync(failing.Id, It.IsAny<CancellationToken>()), Times.Never);
        _publisher.Verify(p => p.PublishAsync(third, It.IsAny<CancellationToken>()), Times.Never);
        _store.Verify(s => s.MarkProcessedAsync(third.Id, It.IsAny<CancellationToken>()), Times.Never);
    }
}
