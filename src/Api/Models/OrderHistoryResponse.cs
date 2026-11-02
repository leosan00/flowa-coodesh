using System.Text.Json.Serialization;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Api.Models;

public sealed record OrderHistoryResponse(
    [property: JsonPropertyName("ordem_id")] Guid OrdemId,
    [property: JsonPropertyName("ativo")] string Ativo,
    [property: JsonPropertyName("lado")] string Lado,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco,
    [property: JsonPropertyName("aceita")] bool Aceita,
    [property: JsonPropertyName("exposicao_atual")] decimal ExposicaoAtual,
    [property: JsonPropertyName("msg_erro")] string? MsgErro,
    [property: JsonPropertyName("registrada_em")] DateTimeOffset RegistradaEm)
{
    public static OrderHistoryResponse From(OrderHistoryItem item) => new(
        item.OrderId,
        item.Symbol.ToString(),
        item.Side == Side.Buy ? "C" : "V",
        item.Quantity,
        item.Price,
        item.Accepted,
        item.Exposure,
        item.Reason,
        item.OccurredAt);
}
