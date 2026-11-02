namespace OrderAccumulator.Application.UseCases.GetOrderHistory.Models;

public sealed record GetOrderHistoryRequestModel(int Take = GetOrderHistoryRequestModel.DefaultTake)
{
    public const int DefaultTake = 50;
    public const int MaxTake = 500;
}
