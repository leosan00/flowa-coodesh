using OrderAccumulator.Domain.Enums;
using static OrderAccumulator.Domain.PtBrFormat;

namespace OrderAccumulator.Domain;

/// <summary>
/// Exposição financeira de um ativo: soma de (preço × quantidade) das compras menos a das vendas.
/// Garante que o valor absoluto nunca ultrapasse <see cref="ExposureLimit.PerAsset"/>.
/// </summary>
public sealed class AssetExposure
{
    public Symbol Symbol { get; }
    public decimal Value { get; private set; }

    public AssetExposure(Symbol symbol, decimal value = 0m)
    {
        if (!Enum.IsDefined(symbol))
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Ativo inválido.");

        Symbol = symbol;
        Value = value;
    }

    /// <summary>
    /// Aplica a ordem se o novo valor absoluto da exposição não ultrapassar o limite
    /// (exatamente o limite é aceito). Uma ordem rejeitada não altera a exposição.
    /// </summary>
    public OrderResult Apply(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Symbol != Symbol)
            throw new ArgumentException($"Ordem de {order.Symbol} não pode ser aplicada à exposição de {Symbol}.", nameof(order));

        var proposed = Value + order.SignedNotional;

        if (Math.Abs(proposed) > ExposureLimit.PerAsset)
        {
            return new OrderResult.Rejected(order, Value,
                $"A exposição de {Symbol} passaria de {Money(Value)} para {Money(proposed)}, " +
                $"ultrapassando o limite de {Money(ExposureLimit.PerAsset)} por ativo.");
        }

        Value = proposed;
        return new OrderResult.Accepted(order, Value);
    }
}
