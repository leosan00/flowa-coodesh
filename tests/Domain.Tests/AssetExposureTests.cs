using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Domain.Events;

namespace OrderAccumulator.Domain.Tests;

public class AssetExposureTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order Buy(int quantity, decimal price, Symbol symbol = Symbol.PETR4) =>
        Order.Create(symbol, Side.Buy, quantity, price, Now);

    private static Order Sell(int quantity, decimal price, Symbol symbol = Symbol.PETR4) =>
        Order.Create(symbol, Side.Sell, quantity, price, Now);

    [Fact]
    public void Buy_increases_exposure()
    {
        var exposure = new AssetExposure(Symbol.PETR4);

        var result = exposure.Apply(Buy(584, 54.87m));

        var accepted = Assert.IsType<OrderResult.Accepted>(result);
        Assert.Equal(32044.08m, accepted.Exposure);
        Assert.Equal(32044.08m, exposure.Value);
    }

    [Fact]
    public void Sell_decreases_exposure_and_can_go_negative()
    {
        var exposure = new AssetExposure(Symbol.PETR4);

        var result = exposure.Apply(Sell(100, 10m));

        Assert.True(result.IsAccepted);
        Assert.Equal(-1000m, exposure.Value);
    }

    [Fact]
    public void Buy_and_sell_net_out()
    {
        var exposure = new AssetExposure(Symbol.PETR4);

        exposure.Apply(Buy(100, 10m));
        exposure.Apply(Sell(40, 10m));

        Assert.Equal(600m, exposure.Value);
    }

    [Fact]
    public void Order_that_reaches_exactly_the_limit_is_accepted()
    {
        var exposure = new AssetExposure(Symbol.PETR4);

        // 1000 x 999.99 = 999.990,00 ; + 10 x 1.00 = 1.000.000,00 exato
        exposure.Apply(Buy(1000, 999.99m));
        var result = exposure.Apply(Buy(10, 1.00m));

        Assert.True(result.IsAccepted);
        Assert.Equal(ExposureLimit.PerAsset, exposure.Value);
    }

    [Fact]
    public void Order_one_cent_above_the_limit_is_rejected_and_not_registered()
    {
        var exposure = new AssetExposure(Symbol.PETR4, ExposureLimit.PerAsset);

        var result = exposure.Apply(Buy(1, 0.01m));

        var rejected = Assert.IsType<OrderResult.Rejected>(result);
        Assert.Equal(ExposureLimit.PerAsset, rejected.Exposure);
        Assert.Equal(ExposureLimit.PerAsset, exposure.Value);
        Assert.Contains("1.000.000,00", rejected.Reason);
        Assert.Contains("PETR4", rejected.Reason);
    }

    [Fact]
    public void Limit_applies_to_the_absolute_value_on_the_sell_side()
    {
        var exposure = new AssetExposure(Symbol.VALE3, -ExposureLimit.PerAsset);

        var result = exposure.Apply(Sell(1, 0.01m, Symbol.VALE3));

        Assert.False(result.IsAccepted);
        Assert.Equal(-ExposureLimit.PerAsset, exposure.Value);
    }

    [Fact]
    public void Sell_that_reduces_exposure_is_accepted_even_when_at_the_limit()
    {
        var exposure = new AssetExposure(Symbol.PETR4, ExposureLimit.PerAsset);

        var result = exposure.Apply(Sell(1, 0.01m));

        Assert.True(result.IsAccepted);
        Assert.Equal(ExposureLimit.PerAsset - 0.01m, exposure.Value);
    }

    [Fact]
    public void Apply_throws_when_order_symbol_differs()
    {
        var exposure = new AssetExposure(Symbol.PETR4);

        Assert.Throws<ArgumentException>(() => exposure.Apply(Buy(1, 1m, Symbol.VALE3)));
    }

    [Fact]
    public void Result_converts_to_the_matching_event()
    {
        var exposure = new AssetExposure(Symbol.PETR4, ExposureLimit.PerAsset);
        var rejected = exposure.Apply(Buy(1, 0.01m));
        var accepted = new AssetExposure(Symbol.PETR4).Apply(Buy(1, 1m));

        var rejectedEvent = Assert.IsType<OrderRejected>(rejected.ToEvent(Now));
        var acceptedEvent = Assert.IsType<OrderAccepted>(accepted.ToEvent(Now));

        Assert.Equal(ExposureLimit.PerAsset, rejectedEvent.CurrentExposure);
        Assert.Equal(1m, acceptedEvent.ExposureAfter);
        Assert.Equal(Now, acceptedEvent.OccurredAt);
    }
}
