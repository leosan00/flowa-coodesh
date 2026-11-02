using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Domain;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Infrastructure.Messaging;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class ExposureRepository(AppDbContext db, TimeProvider clock) : IExposureRepository
{
    public async Task<OrderResult> PlaceAsync(Order order, CancellationToken cancellationToken = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var row = await LoadRowLockedAsync(order.Symbol, cancellationToken);

        var exposure = new AssetExposure(order.Symbol, row.Value);
        var result = exposure.Apply(order);

        row.Value = exposure.Value;
        db.Outbox.Add(OutboxEventSerializer.ToOutboxMessage(result.ToEvent(clock.GetUtcNow())));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return result;
    }

    private async Task<AssetExposureRow> LoadRowLockedAsync(Symbol symbol, CancellationToken cancellationToken)
    {
        var rows = await db.Exposures
            .FromSqlInterpolated($"SELECT [Symbol], [Value] FROM [AssetExposures] WITH (UPDLOCK, ROWLOCK) WHERE [Symbol] = {symbol.ToString()}")
            .ToListAsync(cancellationToken);

        return rows.Single();
    }
}
