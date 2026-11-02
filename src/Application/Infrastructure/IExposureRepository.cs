using OrderAccumulator.Domain;

namespace OrderAccumulator.Application.Infrastructure;

public interface IExposureRepository
{
    Task<OrderResult> PlaceAsync(Order order, CancellationToken cancellationToken = default);
}
