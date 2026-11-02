using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderAccumulator.Infrastructure.Persistence;

/// <summary>
/// Usada só pelas ferramentas do EF (<c>dotnet ef migrations add</c>), para gerar as migrations
/// sem subir a aplicação nem conectar ao banco. As migrations são do SQL Server.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=OrderAccumulator;User Id=sa;Password=design-time;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }
}
