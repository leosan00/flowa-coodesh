using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Infrastructure.Persistence.Entities;

public sealed class OrderHistoryRow
{
    public Guid EventId { get; set; }
    public Guid OrderId { get; set; }
    public Symbol Symbol { get; set; }
    public Side Side { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public bool Accepted { get; set; }
    public decimal Exposure { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
