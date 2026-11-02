using Moq;
using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Events;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Tests;

public class OrderHistoryProjectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IOrderHistoryStore> _store = new();

    private OrderHistoryProjector CreateProjector() => new(_store.Object);

    private static (IDomainEvent Event, string Json) AcceptedEvent()
    {
        var order = Order.Create(Symbol.PETR4, Side.Buy, 584, 54.87m, Now);
        var domainEvent = new OrderResult.Accepted(order, 32044.08m).ToEvent(Now);
        return (domainEvent, OutboxEventSerializer.ToOutboxMessage(domainEvent).Payload);
    }

    private static string RejectedJson()
    {
        var order = Order.Create(Symbol.VALE3, Side.Sell, 10, 1.01m, Now);
        var domainEvent = new OrderResult.Rejected(order, -999_990m, "acima do limite").ToEvent(Now);
        return OutboxEventSerializer.ToOutboxMessage(domainEvent).Payload;
    }

    [Fact]
    public async Task Accepted_event_becomes_an_accepted_history_row()
    {
        OrderHistoryRow? stored = null;
        _store
            .Setup(s => s.TryAddAsync(It.IsAny<OrderHistoryRow>(), It.IsAny<CancellationToken>()))
            .Callback((OrderHistoryRow row, CancellationToken _) => stored = row)
            .ReturnsAsync(true);
        var (domainEvent, json) = AcceptedEvent();

        var result = await CreateProjector().ProjectAsync(json);

        Assert.True(result);
        Assert.NotNull(stored);
        Assert.Equal(domainEvent.EventId, stored.EventId);
        Assert.Equal(Symbol.PETR4, stored.Symbol);
        Assert.Equal(Side.Buy, stored.Side);
        Assert.Equal(584, stored.Quantity);
        Assert.Equal(54.87m, stored.Price);
        Assert.True(stored.Accepted);
        Assert.Equal(32044.08m, stored.Exposure);
        Assert.Null(stored.Reason);
        Assert.Equal(Now.UtcDateTime, stored.OccurredAtUtc);
    }

    [Fact]
    public async Task Rejected_event_becomes_a_rejected_history_row_with_the_reason()
    {
        OrderHistoryRow? stored = null;
        _store
            .Setup(s => s.TryAddAsync(It.IsAny<OrderHistoryRow>(), It.IsAny<CancellationToken>()))
            .Callback((OrderHistoryRow row, CancellationToken _) => stored = row)
            .ReturnsAsync(true);

        await CreateProjector().ProjectAsync(RejectedJson());

        Assert.NotNull(stored);
        Assert.Equal(Symbol.VALE3, stored.Symbol);
        Assert.Equal(Side.Sell, stored.Side);
        Assert.False(stored.Accepted);
        Assert.Equal(-999_990m, stored.Exposure);
        Assert.Equal("acima do limite", stored.Reason);
    }

    [Fact]
    public async Task Repeated_event_is_reported_as_not_stored()
    {
        _store
            .Setup(s => s.TryAddAsync(It.IsAny<OrderHistoryRow>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var (_, json) = AcceptedEvent();

        var result = await CreateProjector().ProjectAsync(json);

        Assert.False(result);
    }

    [Theory]
    [InlineData("não é json")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Message_that_is_not_an_event_is_rejected_and_never_stored(string json)
    {
        await Assert.ThrowsAsync<InvalidEventException>(() => CreateProjector().ProjectAsync(json));

        _store.Verify(s => s.TryAddAsync(It.IsAny<OrderHistoryRow>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("\"type\":\"OrderAccepted\"", "\"type\":\"OrderSomethingElse\"")]
    [InlineData("\"symbol\":\"PETR4\"", "\"symbol\":\"XXXX3\"")]
    [InlineData("\"symbol\":\"PETR4\"", "\"symbol\":\"petr4\"")]
    [InlineData("\"side\":\"Buy\"", "\"side\":\"Hold\"")]
    public async Task Event_with_unknown_type_symbol_or_side_is_rejected_and_never_stored(string original, string replacement)
    {
        var (_, json) = AcceptedEvent();
        Assert.Contains(original, json);

        await Assert.ThrowsAsync<InvalidEventException>(() => CreateProjector().ProjectAsync(json.Replace(original, replacement)));

        _store.Verify(s => s.TryAddAsync(It.IsAny<OrderHistoryRow>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
