using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Domain;

public sealed record Order
{
    public const int MaxQuantityExclusive = 100_000;
    public const decimal MaxPriceExclusive = 1_000m;
    public const decimal PriceStep = 0.01m;

    public Guid Id { get; }
    public Symbol Symbol { get; }
    public Side Side { get; }
    public int Quantity { get; }
    public decimal Price { get; }
    public DateTimeOffset CreatedAt { get; }

    public decimal Notional => Price * Quantity;

    public decimal SignedNotional => Side == Side.Buy ? Notional : -Notional;

    private Order(Guid id, Symbol symbol, Side side, int quantity, decimal price, DateTimeOffset createdAt)
    {
        Id = id;
        Symbol = symbol;
        Side = side;
        Quantity = quantity;
        Price = price;
        CreatedAt = createdAt;
    }

    public static bool IsValidQuantity(int quantity) =>
        quantity > 0 && quantity < MaxQuantityExclusive;

    public static bool IsValidPrice(decimal price) =>
        price > 0 && price < MaxPriceExclusive && price % PriceStep == 0;

    public static string? QuantityError(int quantity) =>
        IsValidQuantity(quantity)
            ? null
            : $"A quantidade deve ser um inteiro positivo menor que {PtBrFormat.Number(MaxQuantityExclusive, "N0")}.";

    public static string? PriceError(decimal price) =>
        IsValidPrice(price)
            ? null
            : $"O preço deve ser positivo, múltiplo de {PtBrFormat.Number(PriceStep, "N2")} " +
              $"e menor que {PtBrFormat.Number(MaxPriceExclusive, "N2")}.";

    public static Order Create(Symbol symbol, Side side, int quantity, decimal price, DateTimeOffset createdAt)
    {
        if (!Enum.IsDefined(symbol))
            throw new ArgumentOutOfRangeException(nameof(symbol), symbol, "Ativo inválido.");
        if (!Enum.IsDefined(side))
            throw new ArgumentOutOfRangeException(nameof(side), side, "Lado inválido.");
        if (QuantityError(quantity) is { } quantityError)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, quantityError);
        if (PriceError(price) is { } priceError)
            throw new ArgumentOutOfRangeException(nameof(price), price, priceError);

        return new Order(Guid.CreateVersion7(), symbol, side, quantity, price, createdAt);
    }
}
