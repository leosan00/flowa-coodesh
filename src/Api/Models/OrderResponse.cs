using System.Text.Json.Serialization;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;

namespace OrderAccumulator.Api.Models;

public sealed record OrderResponse(
    [property: JsonPropertyName("sucesso")] bool Sucesso,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual,
    [property: JsonPropertyName("msg_erro")] string? MsgErro)
{
    public static OrderResponse From(PlaceOrderResponseModel output) => new(output.Success, output.Exposure, output.Error);

    public static OrderResponse BadRequest(string message) => new(false, 0m, message);
}
