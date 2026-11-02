using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Infrastructure.Persistence;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Messaging;

public sealed class OrderHistoryStore(AppDbContext db) : IOrderHistoryStore
{
    private const int PrimaryKeyViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task<bool> TryAddAsync(OrderHistoryRow row, CancellationToken cancellationToken = default)
    {
        db.OrderHistory.Add(row);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: PrimaryKeyViolation or UniqueIndexViolation })
        {
            db.Entry(row).State = EntityState.Detached;
            return false;
        }
    }
}
