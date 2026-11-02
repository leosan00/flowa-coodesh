using System.Text.Json;
using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Events;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public static class OutboxEventSerializer
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static OutboxMessage ToOutboxMessage(IDomainEvent domainEvent)
    {
        var payload = ToPayload(domainEvent);

        return new OutboxMessage
        {
            Id = payload.EventId,
            Type = payload.Type,
            Key = payload.Symbol,
            Payload = JsonSerializer.Serialize(payload, JsonOptions),
            OccurredAtUtc = payload.OccurredAt.UtcDateTime
        };
    }

    public static OrderEventPayload? Deserialize(string json) =>
        JsonSerializer.Deserialize<OrderEventPayload>(json, JsonOptions);

    private static OrderEventPayload ToPayload(IDomainEvent domainEvent) => domainEvent switch
    {
        OrderAccepted accepted => Build(accepted, accepted.Order, accepted.ExposureAfter, null),
        OrderRejected rejected => Build(rejected, rejected.Order, rejected.CurrentExposure, rejected.Reason),
        _ => throw new NotSupportedException($"Evento de domínio sem serialização: {domainEvent.GetType().Name}.")
    };

    private static OrderEventPayload Build(IDomainEvent domainEvent, Order order, decimal exposure, string? reason) =>
        new(
            domainEvent.EventId,
            domainEvent.GetType().Name,
            domainEvent.OccurredAt,
            order.Id,
            order.Symbol.ToString(),
            order.Side.ToString(),
            order.Quantity,
            order.Price,
            exposure,
            reason);
}
