namespace OrderAccumulator.Infrastructure.Messaging;

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
