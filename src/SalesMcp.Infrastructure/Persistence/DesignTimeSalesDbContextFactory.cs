using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SalesMcp.Infrastructure.Persistence;

/// <summary>
/// Lets <c>dotnet ef</c> create the context without a host. Used only to build migrations:
/// <c>dotnet ef migrations add Name --project src/SalesMcp.Infrastructure --output-dir Migrations/Sqlite</c>.
/// </summary>
internal sealed class DesignTimeSalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite("Data Source=:memory:").Options);
}
