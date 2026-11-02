using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

/// <summary>Acesso do relay à tabela Outbox.</summary>
public interface IOutboxStore
{
    /// <summary>Mensagens ainda não publicadas, da mais antiga para a mais recente.</summary>
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default);

    Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default);
}
