using OrderAccumulator.Domain.Events;

namespace OrderAccumulator.Domain;

/// <summary>
/// Resultado de aplicar uma ordem a um <see cref="AssetExposure"/>.
/// <see cref="Exposure"/> é a exposição depois da ordem quando aceita e a exposição atual (inalterada) quando rejeitada.
/// </summary>
public abstract record OrderResult(Order Order, decimal Exposure)
{
    public bool IsAccepted => this is Accepted;

    public IDomainEvent ToEvent(DateTimeOffset occurredAt) => this switch
    {
        Accepted a => new OrderAccepted(a.Order, a.Exposure, occurredAt),
        Rejected r => new OrderRejected(r.Order, r.Exposure, r.Reason, occurredAt),
        _ => throw new InvalidOperationException("Resultado de ordem desconhecido.")
    };

    public sealed record Accepted(Order Order, decimal Exposure) : OrderResult(Order, Exposure);

    public sealed record Rejected(Order Order, decimal Exposure, string Reason) : OrderResult(Order, Exposure);
}
