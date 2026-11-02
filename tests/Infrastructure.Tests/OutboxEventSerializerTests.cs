using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Infrastructure.Messaging;

namespace OrderAccumulator.Infrastructure.Tests;

public class OutboxEventSerializerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Accepted_event_is_serialized_with_the_exposure_after_the_order()
    {
        var order = Order.Create(Symbol.PETR4, Side.Buy, 584, 54.87m, Now);
        var domainEvent = new OrderResult.Accepted(order, 32044.08m).ToEvent(Now);

        var message = OutboxEventSerializer.ToOutboxMessage(domainEvent);

        Assert.Equal(domainEvent.EventId, message.Id);
        Assert.Equal("OrderAccepted", message.Type);
        Assert.Equal("PETR4", message.Key);
        Assert.Equal(Now.UtcDateTime, message.OccurredAtUtc);
        Assert.Null(message.ProcessedAtUtc);

        var payload = OutboxEventSerializer.Deserialize(message.Payload);
        Assert.NotNull(payload);
        Assert.Equal(domainEvent.EventId, payload.EventId);
        Assert.Equal(order.Id, payload.OrderId);
        Assert.Equal("PETR4", payload.Symbol);
        Assert.Equal("Buy", payload.Side);
        Assert.Equal(584, payload.Quantity);
        Assert.Equal(54.87m, payload.Price);
        Assert.Equal(32044.08m, payload.Exposure);
        Assert.Null(payload.Reason);
    }

    [Fact]
    public void Rejected_event_is_serialized_with_the_current_exposure_and_the_reason()
    {
        var order = Order.Create(Symbol.VALE3, Side.Sell, 10, 1.01m, Now);
        var domainEvent = new OrderResult.Rejected(order, -999_990m, "acima do limite").ToEvent(Now);

        var message = OutboxEventSerializer.ToOutboxMessage(domainEvent);

        Assert.Equal("OrderRejected", message.Type);
        Assert.Equal("VALE3", message.Key);

        var payload = OutboxEventSerializer.Deserialize(message.Payload);
        Assert.NotNull(payload);
        Assert.Equal("Sell", payload.Side);
        Assert.Equal(-999_990m, payload.Exposure);
        Assert.Equal("acima do limite", payload.Reason);
    }
}
