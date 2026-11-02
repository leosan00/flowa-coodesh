using Moq;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Application.UseCases.GetExposures;
using OrderAccumulator.Application.UseCases.GetExposures.Models;
using OrderAccumulator.Application.UseCases.GetOrderHistory;
using OrderAccumulator.Application.UseCases.GetOrderHistory.Models;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Application.Tests;

public class QueryUseCasesTests
{
    private readonly Mock<IExposureReadModel> _readModel = new();

    [Fact]
    public async Task GetExposures_returns_every_symbol_with_zero_for_the_unknown_ones()
    {
        _readModel
            .Setup(r => r.GetExposuresAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ExposureView(Symbol.VALE3, 250m)]);

        var result = await new GetExposuresUseCase(_readModel.Object).ExecuteAsync(new GetExposuresRequestModel());

        Assert.Equal(3, result.Count);
        Assert.Equal(0m, result.Single(e => e.Symbol == Symbol.PETR4).Value);
        Assert.Equal(250m, result.Single(e => e.Symbol == Symbol.VALE3).Value);
        Assert.Equal(0m, result.Single(e => e.Symbol == Symbol.VIIA4).Value);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(20, 20)]
    [InlineData(10_000, GetOrderHistoryRequestModel.MaxTake)]
    public async Task GetOrderHistory_clamps_take(int requested, int expected)
    {
        _readModel
            .Setup(r => r.GetOrderHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await new GetOrderHistoryUseCase(_readModel.Object).ExecuteAsync(new GetOrderHistoryRequestModel(requested));

        _readModel.Verify(r => r.GetOrderHistoryAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetOrderHistory_returns_what_the_read_model_has()
    {
        var item = new OrderHistoryItem(Guid.NewGuid(), Symbol.PETR4, Side.Buy, 1, 1m, true, 1m, null, DateTimeOffset.UtcNow);
        _readModel
            .Setup(r => r.GetOrderHistoryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([item]);

        var result = await new GetOrderHistoryUseCase(_readModel.Object).ExecuteAsync(new GetOrderHistoryRequestModel());

        Assert.Same(item, Assert.Single(result));
    }
}
