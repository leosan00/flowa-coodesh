using OrderAccumulator.Domain.Events;

namespace OrderAccumulator.Domain;

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
