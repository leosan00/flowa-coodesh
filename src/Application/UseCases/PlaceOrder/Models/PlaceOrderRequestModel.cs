using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.UseCases.PlaceOrder.Models;

public sealed record PlaceOrderRequestModel(Symbol Symbol, Side Side, int Quantity, decimal Price);
