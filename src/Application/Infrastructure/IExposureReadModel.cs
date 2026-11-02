using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.Infrastructure;

public interface IExposureReadModel
{
    Task<decimal> GetExposureAsync(Symbol symbol, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExposureView>> GetExposuresAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderHistoryItem>> GetOrderHistoryAsync(int take, CancellationToken cancellationToken = default);
}
