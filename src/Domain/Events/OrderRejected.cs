namespace OrderAccumulator.Domain.Events;

public sealed record OrderRejected(Order Order, decimal CurrentExposure, string Reason, DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
