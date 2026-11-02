using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OutboxStore(AppDbContext db, TimeProvider clock) : IOutboxStore
{
    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int take, CancellationToken cancellationToken = default) =>
        await db.Outbox
            .AsNoTracking()
            .Where(message => message.ProcessedAtUtc == null)
            .OrderBy(message => message.OccurredAtUtc)
            .ThenBy(message => message.Id)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task MarkProcessedAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow().UtcDateTime;

        await db.Outbox
            .Where(message => message.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(message => message.ProcessedAtUtc, now), cancellationToken);
    }
}
