using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases.GetOrderHistory.Models;

namespace OrderAccumulator.Application.UseCases.GetOrderHistory;

public sealed class GetOrderHistoryUseCase(IExposureReadModel readModel)
    : IUseCase<GetOrderHistoryRequestModel, IReadOnlyList<OrderHistoryItem>>
{
    public Task<IReadOnlyList<OrderHistoryItem>> ExecuteAsync(GetOrderHistoryRequestModel input, CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(input.Take, 1, GetOrderHistoryRequestModel.MaxTake);
        return readModel.GetOrderHistoryAsync(take, cancellationToken);
    }
}
