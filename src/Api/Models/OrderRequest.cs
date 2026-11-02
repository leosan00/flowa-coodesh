using System.Text.Json.Serialization;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Api.Models;

public record OrderRequest(
    [property: JsonPropertyName("ativo")] string? Ativo,
    [property: JsonPropertyName("lado")] string? Lado,
    [property: JsonPropertyName("quantidade")] int Quantidade,
    [property: JsonPropertyName("preco")] decimal Preco)
{

    public string? TryToInput()
    {
        string? error = null;

        var symbolName = Enum.GetNames<Symbol>().FirstOrDefault(name => name == Ativo);
        if (symbolName is null)
        {
            return error = $"Ativo inválido. Use {string.Join(", ", Enum.GetNames<Symbol>())}.";
        }

        if (Lado != "C" && Lado != "V")
        {
            return error = "Lado inválido. Use \"C\" (compra) ou \"V\" (venda).";
        }

        return error;
    }

    public PlaceOrderRequestModel ToInput() => new(
        Enum.Parse<Symbol>(Ativo!),
        Lado == "C" ? Side.Buy : Side.Sell,
        Quantidade,
        Preco);
}
