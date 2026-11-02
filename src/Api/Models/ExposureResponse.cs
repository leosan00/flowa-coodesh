using System.Text.Json.Serialization;
using OrderAccumulator.Application.ReadModels;

namespace OrderAccumulator.Api.Models;

public sealed record ExposureResponse(
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual)
{
    public static ExposureResponse From(ExposureView view) => new(view.Symbol.ToString(), view.Value);
}
