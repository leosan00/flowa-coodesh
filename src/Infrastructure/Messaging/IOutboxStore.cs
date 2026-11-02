using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public interface IOutboxStore
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}
