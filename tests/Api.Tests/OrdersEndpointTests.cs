using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Moq;
using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Api.Tests;

/// <summary>
/// Testa o contrato HTTP (status, JSON e validação da requisição) com os repositórios simulados.
/// A regra do limite está nos testes de Domain e Application; o banco, fora deste projeto.
/// </summary>
public sealed class OrdersEndpointTests : IClassFixture<ApiFactory>, IAsyncLifetime, IDisposable
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public OrdersEndpointTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync()
    {
        _factory.ResetMocks();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public void Dispose() => _client.Dispose();

    private sealed record Result(HttpStatusCode Status, JsonElement Body)
    {
        public bool Sucesso => Body.GetProperty("sucesso").GetBoolean();
        public decimal Exposicao => Body.GetProperty("exposicao_atual").GetDecimal();
        public string? Erro => Body.GetProperty("msg_erro").GetString();
    }

    private async Task<Result> PostAsync(string? ativo, string? lado, int quantidade, decimal preco) =>
        await ReadAsync(await _client.PostAsJsonAsync("/orders", new { ativo, lado, quantidade, preco }));

    private async Task<Result> PostRawAsync(string json) =>
        await ReadAsync(await _client.PostAsync("/orders", new StringContent(json, Encoding.UTF8, "application/json")));

    private static async Task<Result> ReadAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return new Result(response.StatusCode, body);
    }

    private void VerifyRepositoryNeverCalled() =>
        _factory.Repository.Verify(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);

    [Fact]
    public async Task Accepted_order_returns_200_with_the_documented_json()
    {
        _factory.Repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => new OrderResult.Accepted(order, 32044.08m));

        var result = await PostAsync("PETR4", "C", 584, 54.87m);

        Assert.Equal(HttpStatusCode.OK, result.Status);
        Assert.True(result.Sucesso);
        Assert.Equal(32044.08m, result.Exposicao);
        Assert.Null(result.Erro);
        Assert.Equal(3, result.Body.EnumerateObject().Count());
    }

    [Fact]
    public async Task Rejected_order_returns_409_with_the_current_exposure_and_the_reason()
    {
        _factory.Repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => new OrderResult.Rejected(order, 999_990m, "acima do limite"));

        var result = await PostAsync("VALE3", "C", 10, 1.01m);

        Assert.Equal(HttpStatusCode.Conflict, result.Status);
        Assert.False(result.Sucesso);
        Assert.Equal(999_990m, result.Exposicao);
        Assert.Equal("acima do limite", result.Erro);
    }

    [Theory]
    [InlineData("XXXX3", "C", 10)]      
    [InlineData("PETR4", "C", 100000)] 
    public async Task Invalid_order_returns_400_and_never_reaches_the_repository(string ativo, string lado, int quantidade)
    {
        var result = await PostAsync(ativo, lado, quantidade, 10.00m);

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.False(result.Sucesso);
        Assert.False(string.IsNullOrWhiteSpace(result.Erro));
        VerifyRepositoryNeverCalled();
    }

    [Fact]
    public async Task Malformed_json_returns_400_in_the_same_format()
    {
        var result = await PostRawAsync("{\"ativo\":\"PETR4\",\"lado\":\"C\",\"quantidade\":\"abc\",\"preco\":10.00}");

        Assert.Equal(HttpStatusCode.BadRequest, result.Status);
        Assert.False(result.Sucesso);
        Assert.Contains("Quantidade inválida", result.Erro);
        VerifyRepositoryNeverCalled();
    }

    [Fact]
    public async Task Request_is_translated_to_the_order_sent_to_the_repository()
    {
        Order? received = null;
        _factory.Repository
            .Setup(r => r.PlaceAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback((Order order, CancellationToken _) => received = order)
            .ReturnsAsync((Order order, CancellationToken _) => new OrderResult.Accepted(order, 1m));

        await PostAsync("VALE3", "V", 584, 54.87m);

        Assert.NotNull(received);
        Assert.Equal(Symbol.VALE3, received.Symbol);
        Assert.Equal(Side.Sell, received.Side);
        Assert.Equal(584, received.Quantity);
        Assert.Equal(54.87m, received.Price);
    }
}
