using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Application.Infrastructure;
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
}
