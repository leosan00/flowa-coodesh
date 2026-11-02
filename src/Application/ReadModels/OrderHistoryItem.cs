using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.ReadModels;

public sealed record OrderHistoryItem(
    Guid OrderId,
    Symbol Symbol,
    Side Side,
    int Quantity,
    decimal Price,
    bool Accepted,
    decimal Exposure,
    string? Reason,
    DateTimeOffset OccurredAt);
