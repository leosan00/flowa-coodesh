namespace OrderAccumulator.Application.UseCases.PlaceOrder.Models;

public enum PlaceOrderStatus
{
    Accepted,

    Rejected,

    Invalid
}

public sealed record PlaceOrderResponseModel(PlaceOrderStatus Status, decimal Exposure, string? Error)
{
    public bool Success => Status == PlaceOrderStatus.Accepted;

    public static PlaceOrderResponseModel Accepted(decimal exposure) => new(PlaceOrderStatus.Accepted, exposure, null);

    public static PlaceOrderResponseModel Rejected(decimal exposure, string reason) => new(PlaceOrderStatus.Rejected, exposure, reason);

    public static PlaceOrderResponseModel Invalid(decimal exposure, string reason) => new(PlaceOrderStatus.Invalid, exposure, reason);
}
