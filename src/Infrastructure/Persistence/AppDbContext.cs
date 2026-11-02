using Microsoft.EntityFrameworkCore;
using OrderAccumulator.Domain.Enums;
using OrderAccumulator.Infrastructure.Persistence.Entities;

namespace OrderAccumulator.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AssetExposureRow> Exposures => Set<AssetExposureRow>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AssetExposureRow>(entity =>
        {
            entity.ToTable("AssetExposures");
            entity.HasKey(e => e.Symbol);
            entity.Property(e => e.Symbol).HasConversion<string>().HasMaxLength(16);
            entity.Property(e => e.Value).HasPrecision(18, 2);

            entity.HasData(Enum.GetValues<Symbol>().Select(symbol => new AssetExposureRow { Symbol = symbol, Value = 0m }));
        });

        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("Outbox");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Type).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Key).HasMaxLength(16).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.HasIndex(e => e.ProcessedAtUtc);
        });
    }
}
