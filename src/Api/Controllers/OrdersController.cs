using Microsoft.AspNetCore.Mvc;
using OrderAccumulator.Api.Models;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases;
using OrderAccumulator.Application.UseCases.GetOrderHistory.Models;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;

namespace OrderAccumulator.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController() : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<OrderResponse>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] OrderRequest request,
        [FromServices] IUseCase<PlaceOrderRequestModel, PlaceOrderResponseModel> placeOrder,
        CancellationToken cancellationToken)
    {
        var error = request.TryToInput();
        if (error is not null)
            return BadRequest(OrderResponse.BadRequest(error));

        var output = await placeOrder.ExecuteAsync(request.ToInput(), cancellationToken);
        var response = OrderResponse.From(output);

        return output.Status switch
        {
            PlaceOrderStatus.Accepted => Ok(response),
            PlaceOrderStatus.Rejected => Conflict(response),
            _ => BadRequest(response)
        };
    }

    /// <summary>Ordens processadas (aceitas e rejeitadas), da mais recente para a mais antiga.</summary>
    /// <param name="take">Quantas ordens devolver (padrão 50, máximo 500).</param>
    [HttpGet]
    [ProducesResponseType<IEnumerable<OrderHistoryResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders(
        [FromServices] IUseCase<GetOrderHistoryRequestModel, IReadOnlyList<OrderHistoryItem>> getHistory,
        CancellationToken cancellationToken,
        [FromQuery] int take = GetOrderHistoryRequestModel.DefaultTake)
    {
        var history = await getHistory.ExecuteAsync(new GetOrderHistoryRequestModel(take), cancellationToken);

        return Ok(history.Select(OrderHistoryResponse.From));
    }
}
