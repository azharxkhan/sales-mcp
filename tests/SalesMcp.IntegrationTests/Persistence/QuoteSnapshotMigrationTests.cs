using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Customers;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.IntegrationTests.Persistence;

/// <summary>
/// The <c>SnapshotQuoteDocumentValues</c> migration fills in, from the current records, what an
/// existing database's quotes didn't store.
/// </summary>
public sealed class QuoteSnapshotMigrationTests
{
    private const string BeforeSnapshot = "20261128192022_AddOfferingCategory";

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Existing_quotes_take_the_current_business_customer_and_units()
    {
        var connectionString = $"Data Source=quotes-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        await using var keepAlive = new SqliteConnection(connectionString);
        await keepAlive.OpenAsync(Ct);
        await using var db = new SalesDbContext(new DbContextOptionsBuilder<SalesDbContext>().UseSqlite(connectionString).Options);
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync(BeforeSnapshot, cancellationToken: Ct);
        await using (var insert = keepAlive.CreateCommand())
        {
            // Only the columns the backfill and the loaded quote need; foreign keys are off on this connection.
            insert.CommandText = """
                PRAGMA foreign_keys = OFF;
                INSERT INTO "Businesses" ("Id", "Name", "Currency", "Locale") VALUES
                    ('10000000-0000-0000-0000-000000000000', 'Acme Distributors', 'ZAR', 'en-ZA');
                INSERT INTO "Customers" ("Id", "BusinessId", "AccountNumber", "Name", "AllowBackOrders", "AllowPartialShipment", "OnHold",
                                         "CreditLimit_Amount", "CreditLimit_Currency", "Balance_Amount", "Balance_Currency",
                                         "PaymentTermsBasis", "PaymentTermsDays") VALUES
                    ('20000000-0000-0000-0000-000000000000', '10000000-0000-0000-0000-000000000000', 'KES-001', 'Kestrel Ridge',
                     1, 1, 0, 0, 'ZAR', 0, 'ZAR', 'DaysFromEndOfMonth', 30);
                INSERT INTO "Offerings" ("Id", "BusinessId", "Sku", "Name", "Description", "VatCode", "IsActive", "Kind", "Unit", "Category") VALUES
                    ('30000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000000', 'ACM-FAS-101', 'Bolts', '', 'Standard', 1, 'Product', 'Box', ''),
                    ('30000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000000', 'ACM-SVC-001', 'Fitting', '', 'Standard', 1, 'Service', NULL, '');
                INSERT INTO "Quotes" ("Id", "Number", "CustomerId", "AgentId", "IssuedOn", "ValidUntil", "Currency", "Status", "RiskReducers") VALUES
                    ('40000000-0000-0000-0000-000000000000', 'Q-2026-000001', '20000000-0000-0000-0000-000000000000',
                     'acme-sales-desk', '2026-03-02', '2026-04-01', 'ZAR', 'Issued', '');
                INSERT INTO "QuoteLines" ("QuoteId", "LineNumber", "OfferingId", "Sku", "Name", "PriceSnapshot") VALUES
                    ('40000000-0000-0000-0000-000000000000', 1, '30000000-0000-0000-0000-000000000001', 'ACM-FAS-101', 'Bolts', '{}'),
                    ('40000000-0000-0000-0000-000000000000', 2, '30000000-0000-0000-0000-000000000002', 'ACM-SVC-001', 'Fitting', '{}');
                """;
            await insert.ExecuteNonQueryAsync(Ct);
        }

        await migrator.MigrateAsync(cancellationToken: Ct);

        await using var read = keepAlive.CreateCommand();
        read.CommandText = """
            SELECT q."BusinessName", q."CustomerName", q."AccountNumber", q."PaymentTermsBasis", q."PaymentTermsDays", l."Unit"
            FROM "Quotes" AS q JOIN "QuoteLines" AS l ON l."QuoteId" = q."Id"
            ORDER BY l."LineNumber"
            """;
        await using var reader = await read.ExecuteReaderAsync(Ct);

        Assert.True(await reader.ReadAsync(Ct));
        Assert.Equal("Acme Distributors", reader.GetString(0));
        Assert.Equal("Kestrel Ridge", reader.GetString(1));
        Assert.Equal("KES-001", reader.GetString(2));
        Assert.Equal(nameof(PaymentTermsBasis.DaysFromEndOfMonth), reader.GetString(3));
        Assert.Equal(30, reader.GetInt32(4));
        Assert.Equal(nameof(UnitOfMeasure.Box), reader.GetString(5));

        Assert.True(await reader.ReadAsync(Ct));
        Assert.True(await reader.IsDBNullAsync(5, Ct));
        Assert.False(await reader.ReadAsync(Ct));
    }
}
