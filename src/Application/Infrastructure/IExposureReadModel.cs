using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.Infrastructure;

public interface IExposureReadModel
{
    Task<decimal> GetExposureAsync(Symbol symbol, CancellationToken cancellationToken = default);
}
