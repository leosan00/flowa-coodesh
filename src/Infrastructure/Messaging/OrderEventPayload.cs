namespace OrderAccumulator.Infrastructure.Messaging;

/// <summary>
/// Formato do evento no outbox e no Kafka (JSON). É um contrato plano, independente das classes do Domain,
/// para que quem consome não precise reconstruir a <c>Order</c>.
/// </summary>
public sealed record OrderEventPayload(
    Guid EventId,
    string Type,
    DateTimeOffset OccurredAt,
    Guid OrderId,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    decimal Exposure,
    string? Reason);
