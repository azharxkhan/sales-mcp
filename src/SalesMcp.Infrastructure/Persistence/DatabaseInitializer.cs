using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SalesMcp.Infrastructure.Seeding;

namespace SalesMcp.Infrastructure.Persistence;

/// <summary>
/// Brings the database up to date at start-up: applies pending migrations, then seeds Acme
/// Distributors if it isn't there yet. Both are no-ops on later runs, so start-up stays fast.
/// </summary>
/// <param name="db">The context to migrate.</param>
/// <param name="seeder">Writes the demo data once.</param>
/// <param name="time">The clock that decides the seed's <c>today</c>.</param>
/// <param name="logger">Start-up messages. In the stdio host these go to stderr.</param>
public sealed partial class DatabaseInitializer(
    SalesDbContext db,
    AcmeSeeder seeder,
    TimeProvider time,
    ILogger<DatabaseInitializer> logger)
{
    /// <summary>Migrates the schema and seeds the demo data if needed.</summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false)).ToList();
        if (pending.Count > 0)
        {
            await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
            LogMigrated(pending.Count);
        }

        // The seed's dates are relative to the business's local day.
        var today = DateOnly.FromDateTime(time.GetLocalNow().DateTime);
        var seeded = await seeder.SeedAsync(today, cancellationToken).ConfigureAwait(false);
        if (seeded && logger.IsEnabled(LogLevel.Information))
        {
            var day = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            LogSeeded(day);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Applied {Count} database migration(s).")]
    private partial void LogMigrated(int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded the Acme Distributors demo data as of {Today}.")]
    private partial void LogSeeded(string today);
}
