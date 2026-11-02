using System.Globalization;
using Moq;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.UseCases.PlaceOrder;
using OrderAccumulator.Application.UseCases.PlaceOrder.Models;
using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.Tests;

public class PlaceOrderUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IExposureRepository> _repository = new(MockBehavior.Strict);
    private readonly Mock<IExposureReadModel> _readModel = new();
    private readonly Mock<TimeProvider> _clock = new();

    public PlaceOrderUseCaseTests() => _clock.Setup(c => c.GetUtcNow()).Returns(Now);

    private PlaceOrderUseCase CreateUseCase() => new(_repository.Object, _readModel.Object, _clock.Object);

    [Fact]
    public async Task Accepted_order_returns_the_new_exposure()
    {
        var input = new PlaceOrderRequestModel(Symbol.PETR4, Side.Buy, 584, 54.87m);
        _repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => new OrderResult.Accepted(o, 32044.08m));

        var output = await CreateUseCase().ExecuteAsync(input);

        Assert.Equal(PlaceOrderStatus.Accepted, output.Status);
        Assert.True(output.Success);
        Assert.Equal(32044.08m, output.Exposure);
        Assert.Null(output.Error);
    }

    [Fact]
    public async Task Order_sent_to_the_repository_carries_the_input_and_the_clock_time()
    {
        Order? received = null;
        _repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback((Order o, CancellationToken _) => received = o)
            .ReturnsAsync((Order o, CancellationToken _) => new OrderResult.Accepted(o, 1m));

        await CreateUseCase().ExecuteAsync(new PlaceOrderRequestModel(Symbol.VALE3, Side.Sell, 10, 2.50m));

        _repository.Verify(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.NotNull(received);
        Assert.Equal(Symbol.VALE3, received.Symbol);
        Assert.Equal(Side.Sell, received.Side);
        Assert.Equal(10, received.Quantity);
        Assert.Equal(2.50m, received.Price);
        Assert.Equal(Now, received.CreatedAt);
    }

    [Fact]
    public async Task Rejected_order_returns_the_current_exposure_and_the_reason()
    {
        var input = new PlaceOrderRequestModel(Symbol.VALE3, Side.Buy, 10, 1m);
        _repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => new OrderResult.Rejected(o, 999_999m, "acima do limite"));

        var output = await CreateUseCase().ExecuteAsync(input);

        Assert.Equal(PlaceOrderStatus.Rejected, output.Status);
        Assert.False(output.Success);
        Assert.Equal(999_999m, output.Exposure);
        Assert.Equal("acima do limite", output.Error);
    }

    [Theory]
    [InlineData(0, "10.00")]
    [InlineData(100_000, "10.00")]
    [InlineData(10, "0")]
    [InlineData(10, "1000.00")]
    [InlineData(10, "10.015")]
    public async Task Invalid_order_never_reaches_the_repository(int quantity, string price)
    {
        _readModel
            .Setup(r => r.GetExposureAsync(Symbol.PETR4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(123.45m);
        var input = new PlaceOrderRequestModel(Symbol.PETR4, Side.Buy, quantity, decimal.Parse(price, CultureInfo.InvariantCulture));

        var output = await CreateUseCase().ExecuteAsync(input);

        Assert.Equal(PlaceOrderStatus.Invalid, output.Status);
        Assert.Equal(123.45m, output.Exposure);
        Assert.False(string.IsNullOrWhiteSpace(output.Error));
        _repository.Verify(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Repository_failure_is_not_swallowed()
    {
        _repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("banco indisponível"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateUseCase().ExecuteAsync(new PlaceOrderRequestModel(Symbol.PETR4, Side.Buy, 1, 1m)));
    }
}
