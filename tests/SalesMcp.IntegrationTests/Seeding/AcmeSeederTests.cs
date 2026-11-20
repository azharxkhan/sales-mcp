using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Quotes;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Persistence;

namespace SalesMcp.IntegrationTests.Seeding;

/// <summary>Seeds a real SQLite database and reads the fixtures back through the Application readers.</summary>
public sealed class AcmeSeederTests : IAsyncLifetime
{
    private static readonly DateOnly _today = new(2026, 11, 19);
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;
    private SqliteDatabase _database = null!;

    public async ValueTask InitializeAsync()
    {
        _database = await SqliteDatabase.CreateAsync();
        Assert.True(await _database.InScopeAsync(sp => sp.GetRequiredService<AcmeSeeder>().SeedAsync(_today, Ct)));
    }

    public ValueTask DisposeAsync() => _database.DisposeAsync();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Writes_every_generated_row()
    {
        var expected = AcmeSeed.Build(_today);

        await _database.WithContextAsync(async db =>
        {
            Assert.Equal(expected.Offerings.Count, await db.Offerings.CountAsync(Ct));
            Assert.Equal(expected.Rules.Count, await db.OfferingRules.CountAsync(Ct));
            Assert.Equal(expected.StockLevels.Count, await db.StockLevels.CountAsync(Ct));
            Assert.Equal(expected.ContractPrices.Count, await db.ContractPrices.CountAsync(Ct));
            Assert.Equal(expected.Customers.Count, await db.Customers.CountAsync(Ct));
            Assert.Equal(expected.Contacts.Count, await db.Contacts.CountAsync(Ct));
            Assert.Equal(expected.Quotes.Count, await db.Quotes.CountAsync(Ct));
            Assert.Equal(expected.Orders.Count, await db.SalesOrders.CountAsync(Ct));
            Assert.Equal(
                expected.Quotes.Count(q => q.Status == QuoteStatus.Accepted),
                await db.Quotes.CountAsync(q => q.Status == QuoteStatus.Accepted, Ct));
        });
    }

    [Fact]
    public async Task Seeding_again_changes_nothing()
    {
        var before = await _database.InScopeAsync(sp => sp.GetRequiredService<SalesDbContext>().Quotes.CountAsync(Ct));

        Assert.False(await _database.InScopeAsync(sp => sp.GetRequiredService<AcmeSeeder>().SeedAsync(_today.AddDays(1), Ct)));
        Assert.Equal(before, await _database.InScopeAsync(sp => sp.GetRequiredService<SalesDbContext>().Quotes.CountAsync(Ct)));
    }

    [Fact]
    public async Task Fixtures_read_back_through_the_application_readers()
    {
        await _database.InScopeAsync(async sp =>
        {
            var catalog = sp.GetRequiredService<ICatalogReader>();
            var active = await catalog.ListActiveOfferingsAsync(_fixtures.Business, Ct);
            Assert.Equal(118, active.Count);
            Assert.DoesNotContain(active, o => o.Id == _fixtures.Offerings.Discontinued);

            var rules = await catalog.GetRulesAsync(_fixtures.Business, Ct);
            Assert.Contains(_fixtures.Offerings.Battery, rules.RequiredBy(_fixtures.Offerings.DrillBody));

            var top = await sp.GetRequiredService<ICustomerReader>().GetCustomerAsync(_fixtures.Customers.Top, Ct);
            var sources = await sp.GetRequiredService<IPricingReader>().GetPriceSourcesAsync(top!, Ct);
            Assert.Equal(_fixtures.PriceLists.Default, sources!.DefaultList.Id);
            Assert.Equal(_fixtures.PriceLists.Mining, sources.CustomerList!.Id);
            Assert.Contains(sources.Contracts, c => c.OfferingId == _fixtures.Offerings.DrillBody);

            var stock = await sp.GetRequiredService<IStockReader>().GetStockAsync(_fixtures.Offerings.ShortWithReceipt, Ct);
            Assert.Equal(2, stock.Count);
            Assert.All(stock, s => Assert.Equal(0m, s.Available));
        });
    }
}
