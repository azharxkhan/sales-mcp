using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Persistence;

namespace SalesMcp.IntegrationTests.Seeding;

/// <summary>
/// One fresh SQLite database per test class: migrated with the real migrations, then seeded with
/// Acme Distributors. Seeding takes a moment, so tests in a class share it and must only read.
/// </summary>
public sealed class SeededAcmeDatabase : IAsyncLifetime
{
    /// <summary>The day the database was seeded as of.</summary>
    public static readonly DateOnly Today = new(2026, 11, 19);

    /// <summary>The database.</summary>
    public SqliteDatabase Database { get; private set; } = null!;

    /// <summary>What the first seed call returned: <see langword="true"/> if it wrote rows.</summary>
    public bool SeededRows { get; private set; }

    /// <summary>The same dataset, built in memory, to compare against.</summary>
    public SeedDataset Expected { get; } = AcmeSeed.Build(Today);

    public async ValueTask InitializeAsync()
    {
        Database = await SqliteDatabase.CreateAsync();
        SeededRows = await Database.InScopeAsync(sp =>
            sp.GetRequiredService<AcmeSeeder>().SeedAsync(Today, TestContext.Current.CancellationToken));
    }

    public ValueTask DisposeAsync() => Database.DisposeAsync();
}
