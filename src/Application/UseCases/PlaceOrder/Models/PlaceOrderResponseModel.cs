namespace OrderAccumulator.Application.UseCases.PlaceOrder.Models;

public enum PlaceOrderStatus
{
    /// <summary>Ordem registrada na exposição.</summary>
    Accepted,

    /// <summary>Ordem válida, mas ultrapassaria o limite de exposição; não foi registrada.</summary>
    Rejected,

    /// <summary>Quantidade ou preço fora das regras; a ordem nem chegou a ser avaliada.</summary>
    Invalid
}

/// <param name="Exposure">Exposição do ativo depois da ordem (aceita) ou a atual, inalterada (rejeitada/inválida).</param>
public sealed record PlaceOrderResponseModel(PlaceOrderStatus Status, decimal Exposure, string? Error)
{
    public bool Success => Status == PlaceOrderStatus.Accepted;

    public static PlaceOrderResponseModel Accepted(decimal exposure) => new(PlaceOrderStatus.Accepted, exposure, null);

    public static PlaceOrderResponseModel Rejected(decimal exposure, string reason) => new(PlaceOrderStatus.Rejected, exposure, reason);

    public static PlaceOrderResponseModel Invalid(decimal exposure, string reason) => new(PlaceOrderStatus.Invalid, exposure, reason);
}
