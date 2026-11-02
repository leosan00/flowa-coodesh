using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public interface IOrderHistoryStore
{
    Task<bool> TryAddAsync(OrderHistoryRow row, CancellationToken cancellationToken = default);
}
