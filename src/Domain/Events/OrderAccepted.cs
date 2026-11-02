namespace OrderAccumulator.Domain.Events;

public sealed record OrderAccepted(Order Order, decimal ExposureAfter, DateTimeOffset OccurredAt) : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.CreateVersion7();
}
