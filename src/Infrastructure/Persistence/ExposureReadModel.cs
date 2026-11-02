using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Infrastructure;
using OrderAccumulator.Application.ReadModels;
using OrderAccumulator.Domain.Enums;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class ExposureReadModel(AppDbContext db) : IExposureReadModel
{
    public async Task<decimal> GetExposureAsync(Symbol symbol, CancellationToken cancellationToken = default)
    {
        var row = await db.Exposures
            .AsNoTracking()
            .SingleOrDefaultAsync(e => e.Symbol == symbol, cancellationToken);

        return row?.Value ?? 0m;
    }

    public async Task<IReadOnlyList<ExposureView>> GetExposuresAsync(CancellationToken cancellationToken = default)
    {
        var rows = await db.Exposures.AsNoTracking().ToListAsync(cancellationToken);

        return rows.Select(row => new ExposureView(row.Symbol, row.Value)).ToList();
    }

    public async Task<IReadOnlyList<OrderHistoryItem>> GetOrderHistoryAsync(int take, CancellationToken cancellationToken = default)
    {
        var rows = await db.OrderHistory
            .AsNoTracking()
            .OrderByDescending(row => row.OccurredAtUtc)
            .ThenByDescending(row => row.EventId)
            .Take(take)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new OrderHistoryItem(
                row.OrderId,
                row.Symbol,
                row.Side,
                row.Quantity,
                row.Price,
                row.Accepted,
                row.Exposure,
                row.Reason,
                new DateTimeOffset(DateTime.SpecifyKind(row.OccurredAtUtc, DateTimeKind.Utc))))
            .ToList();
    }
}
