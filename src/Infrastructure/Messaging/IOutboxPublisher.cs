using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public interface IOutboxPublisher
{
    Task PublishAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
