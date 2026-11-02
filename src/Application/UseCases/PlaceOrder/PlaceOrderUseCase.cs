using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;
using OrderAccumulator.Domain;

namespace OrderAccumulator.Application.UseCases.PlaceOrder;

public sealed class PlaceOrderUseCase(
    IExposureRepository exposures,
    IExposureReadModel readModel,
    TimeProvider clock) : IUseCase<PlaceOrderRequestModel, PlaceOrderResponseModel>
{
    public async Task<PlaceOrderResponseModel> ExecuteAsync(PlaceOrderRequestModel input, CancellationToken cancellationToken = default)
    {
        var error = Order.QuantityError(input.Quantity) ?? Order.PriceError(input.Price);
        if (error is not null)
        {
            var current = await readModel.GetExposureAsync(input.Symbol, cancellationToken);
            return PlaceOrderResponseModel.Invalid(current, error);
        }

        var order = Order.Create(input.Symbol, input.Side, input.Quantity, input.Price, clock.GetUtcNow());

        var result = await exposures.PlaceAsync(order, cancellationToken);

        return result switch
        {
            OrderResult.Accepted accepted => PlaceOrderResponseModel.Accepted(accepted.Exposure),
            OrderResult.Rejected rejected => PlaceOrderResponseModel.Rejected(rejected.Exposure, rejected.Reason),
            _ => throw new InvalidOperationException("Resultado de ordem desconhecido.")
        };
    }
}
