namespace OrderAccumulator.Infrastructure.Persistence.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Payload { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
}
