using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Domain.Tests;

public class OrderTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_with_valid_values_exposes_them()
    {
        var order = Order.Create(Symbol.PETR4, Side.Buy, 584, 54.87m, Now);

        Assert.Equal(Symbol.PETR4, order.Symbol);
        Assert.Equal(Side.Buy, order.Side);
        Assert.Equal(584, order.Quantity);
        Assert.Equal(54.87m, order.Price);
        Assert.Equal(32044.08m, order.Notional);
        Assert.NotEqual(Guid.Empty, order.Id);
    }

    [Fact]
    public void Buy_contributes_positive_and_sell_negative_notional()
    {
        var buy = Order.Create(Symbol.VALE3, Side.Buy, 10, 2m, Now);
        var sell = Order.Create(Symbol.VALE3, Side.Sell, 10, 2m, Now);

        Assert.Equal(20m, buy.SignedNotional);
        Assert.Equal(-20m, sell.SignedNotional);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(99_999, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(100_000, false)]
    public void Quantity_must_be_positive_and_below_100000(int quantity, bool expected) =>
        Assert.Equal(expected, Order.IsValidQuantity(quantity));

    [Theory]
    [InlineData("0.01", true)]
    [InlineData("54.87", true)]
    [InlineData("999.99", true)]
    [InlineData("0", false)]
    [InlineData("-1.00", false)]
    [InlineData("1000", false)]
    [InlineData("1000.00", false)]
    [InlineData("10.015", false)]
    [InlineData("0.001", false)]
    public void Price_must_be_positive_multiple_of_a_cent_and_below_1000(string price, bool expected) =>
        Assert.Equal(expected, Order.IsValidPrice(decimal.Parse(price, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void Create_throws_for_invalid_quantity() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create(Symbol.PETR4, Side.Buy, 100_000, 10m, Now));

    [Fact]
    public void Create_throws_for_invalid_price() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create(Symbol.PETR4, Side.Buy, 10, 10.015m, Now));

    [Fact]
    public void Create_throws_for_undefined_symbol_or_side()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create((Symbol)99, Side.Buy, 10, 10m, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => Order.Create(Symbol.PETR4, (Side)99, 10, 10m, Now));
    }
}
