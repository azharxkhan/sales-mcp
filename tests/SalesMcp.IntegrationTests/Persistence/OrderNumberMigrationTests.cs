using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.IntegrationTests.Persistence;

/// <summary>The <c>AddOrderNumbers</c> migration numbers the orders an existing database already has.</summary>
public sealed class OrderNumberMigrationTests
{
    private const string BeforeOrderNumbers = "20261128163511_AddIdempotencyRecords";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Existing_orders_are_numbered_per_year_oldest_first()
    {
        var connectionString = $"Data Source=orders-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(Ct);
        await using var db = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connectionString).Options);
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(BeforeOrderNumbers, cancellationToken: Ct);
        await using (var insert = keepAlive.CreateCommand())
        {
            // Orders only: foreign keys are off on this connection, so no customers or quotes are needed.
            insert.CommandText = """
                PRAGMA foreign_keys = OFF;
                INSERT INTO "SalesOrders" ("Id", "CustomerId", "AgentId", "SourceQuoteId", "OrderedOn", "Currency", "Status") VALUES
                    ('00000000-0000-0000-0000-000000000003', 'C', 'acme-sales-desk', 'Q3', '2026-03-02', 'ZAR', 'Shipped'),
                    ('00000000-0000-0000-0000-000000000001', 'C', 'acme-sales-desk', 'Q1', '2025-12-30', 'ZAR', 'Invoiced'),
                    ('00000000-0000-0000-0000-000000000002', 'C', 'acme-sales-desk', 'Q2', '2026-01-15', 'ZAR', 'Invoiced'),
                    ('00000000-0000-0000-0000-000000000004', 'C', 'acme-sales-desk', 'Q4', '2026-01-15', 'ZAR', 'Draft');
                """;
            await insert.ExecuteNonQueryAsync(Ct);
        }

        await migrator.MigrateAsync(cancellationToken: Ct);

        await using var read = keepAlive.CreateCommand();
        read.CommandText = """SELECT "Id", "Number" FROM "SalesOrders" ORDER BY "Id" """;
        var numbers = new Dictionary<string, string>(StringComparer.Ordinal);
        await using (var reader = await read.ExecuteReaderAsync(Ct))
        {
            while (await reader.ReadAsync(Ct))
            {
                numbers[reader.GetString(0)[^1..]] = reader.GetString(1);
            }
        }

        Assert.Equal("SO-2025-000001", numbers["1"]);
        Assert.Equal("SO-2026-000001", numbers["2"]);
        Assert.Equal("SO-2026-000002", numbers["4"]);
        Assert.Equal("SO-2026-000003", numbers["3"]);
    }
}
