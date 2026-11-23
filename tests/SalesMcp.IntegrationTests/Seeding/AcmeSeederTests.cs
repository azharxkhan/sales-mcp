using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Persistence;

namespace SalesMcp.IntegrationTests.Seeding;

/// <summary>Seeds a real SQLite database once for the class and reads the data back.</summary>
public sealed class AcmeSeederTests(SeededAcmeDatabase seeded) : IClassFixture<SeededAcmeDatabase>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;
    private readonly SqliteDatabase _database = seeded.Database;
    private readonly SeedDataset _expected = seeded.Expected;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public void The_first_seed_writes_rows() => Assert.True(seeded.SeededRows);

    [Fact]
    public async Task Writes_every_generated_row()
    {
        var counts = await CountRowsAsync();

        Assert.Equal(ExpectedCounts(), counts);
        await _database.WithContextAsync(async db => Assert.Equal(
            _expected.Quotes.Count(q => q.Status == QuoteStatus.Accepted),
            await db.Quotes.CountAsync(q => q.Status == QuoteStatus.Accepted, Ct)));
    }

    [Fact]
    public async Task Seeding_again_changes_nothing()
    {
        var before = await CountRowsAsync();

        // A later day would build different history, so a second write would show up in every table.
        Assert.False(await _database.InScopeAsync(sp => sp.GetRequiredService<AcmeSeeder>().SeedAsync(SeededAcmeDatabase.Today.AddDays(1), Ct)));
        Assert.False(await _database.InScopeAsync(sp => sp.GetRequiredService<AcmeSeeder>().SeedAsync(SeededAcmeDatabase.Today, Ct)));

        Assert.Equal(before, await CountRowsAsync());
    }

    [Fact]
    public async Task Money_and_stock_sums_on_sqlite_equal_the_in_memory_sums()
    {
        // SQLite has no decimal type, so sums run over the stored scaled integers. Run under en-ZA,
        // where the decimal separator is a comma, so a text-based mapping would fail on any machine.
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-ZA");
        try
        {
            var fromDb = await _database.InScopeAsync(async sp =>
            {
                var db = sp.GetRequiredService<SalesDbContext>();
                return new Sums(
                    await db.Set<PriceListItem>().SumAsync(i => i.UnitPrice.Amount, Ct),
                    await db.ContractPrices.SumAsync(c => c.UnitPrice.Amount, Ct),
                    await db.Set<OrderLine>().SumAsync(l => l.UnitPrice.Amount, Ct),
                    await db.StockLevels.SumAsync(s => s.OnHand, Ct),
                    await db.StockLevels.SumAsync(s => s.Allocated, Ct),
                    await db.StockLevels.SumAsync(s => s.OnOrder, Ct));
            });

            var inMemory = new Sums(
                _expected.PriceLists.SelectMany(p => p.Items).Sum(i => i.UnitPrice.Amount),
                _expected.ContractPrices.Sum(c => c.UnitPrice.Amount),
                _expected.Orders.SelectMany(o => o.Lines).Sum(l => l.UnitPrice.Amount),
                _expected.StockLevels.Sum(s => s.OnHand),
                _expected.StockLevels.Sum(s => s.Allocated),
                _expected.StockLevels.Sum(s => s.OnOrder));

            Assert.Equal(inMemory, fromDb);
            Assert.True(inMemory.PriceListItems > 0m && inMemory.OnHand > 0m, "The seed should hold prices and stock.");
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public async Task Quotes_and_orders_read_back_with_the_same_totals()
    {
        var (quotes, orders) = await _database.InScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<SalesDbContext>();
            return (await db.Quotes.AsNoTracking().ToListAsync(Ct), await db.SalesOrders.AsNoTracking().ToListAsync(Ct));
        });

        var quotesById = quotes.ToDictionary(q => q.Id);
        Assert.All(_expected.Quotes, expected =>
        {
            var loaded = quotesById[expected.Id];
            Assert.Equal(expected.Status, loaded.Status);
            Assert.Equal(expected.Lines.Count, loaded.Lines.Count);
            AssertSameTotals(expected.Totals, loaded.Totals);
        });

        var ordersById = orders.ToDictionary(o => o.Id);
        Assert.All(_expected.Orders, expected =>
        {
            var loaded = ordersById[expected.Id];
            Assert.Equal(expected.Status, loaded.Status);
            AssertSameTotals(expected.Totals, loaded.Totals);
            Assert.Equal(quotesById[expected.SourceQuoteId].Totals.Total, loaded.Totals.Total);
        });

        Assert.Equal(_expected.Quotes.Sum(q => q.Totals.Total.Amount), quotes.Sum(q => q.Totals.Total.Amount));
        Assert.Equal(_expected.Orders.Sum(o => o.Totals.Total.Amount), orders.Sum(o => o.Totals.Total.Amount));
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

    private static void AssertSameTotals(VatSummary expected, VatSummary actual)
    {
        Assert.Equal(expected.Net, actual.Net);
        Assert.Equal(expected.Vat, actual.Vat);
        Assert.Equal(expected.ByCode, actual.ByCode);
    }

    private Dictionary<string, int> ExpectedCounts() => new()
    {
        ["Businesses"] = 1,
        ["Offerings"] = _expected.Offerings.Count,
        ["OfferingRules"] = _expected.Rules.Count,
        ["Warehouses"] = _expected.Warehouses.Count,
        ["StockLevels"] = _expected.StockLevels.Count,
        ["PriceLists"] = _expected.PriceLists.Count,
        ["PriceListItems"] = _expected.PriceLists.Sum(p => p.Items.Count),
        ["ContractPrices"] = _expected.ContractPrices.Count,
        ["Customers"] = _expected.Customers.Count,
        ["Contacts"] = _expected.Contacts.Count,
        ["Quotes"] = _expected.Quotes.Count,
        ["QuoteLines"] = _expected.Quotes.Sum(q => q.Lines.Count),
        ["SalesOrders"] = _expected.Orders.Count,
        ["OrderLines"] = _expected.Orders.Sum(o => o.Lines.Count),
    };

    private Task<Dictionary<string, int>> CountRowsAsync() =>
        _database.InScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<SalesDbContext>();
            return new Dictionary<string, int>
            {
                ["Businesses"] = await db.Businesses.CountAsync(Ct),
                ["Offerings"] = await db.Offerings.CountAsync(Ct),
                ["OfferingRules"] = await db.OfferingRules.CountAsync(Ct),
                ["Warehouses"] = await db.Warehouses.CountAsync(Ct),
                ["StockLevels"] = await db.StockLevels.CountAsync(Ct),
                ["PriceLists"] = await db.PriceLists.CountAsync(Ct),
                ["PriceListItems"] = await db.Set<PriceListItem>().CountAsync(Ct),
                ["ContractPrices"] = await db.ContractPrices.CountAsync(Ct),
                ["Customers"] = await db.Customers.CountAsync(Ct),
                ["Contacts"] = await db.Contacts.CountAsync(Ct),
                ["Quotes"] = await db.Quotes.CountAsync(Ct),
                ["QuoteLines"] = await db.Set<QuoteLine>().CountAsync(Ct),
                ["SalesOrders"] = await db.SalesOrders.CountAsync(Ct),
                ["OrderLines"] = await db.Set<OrderLine>().CountAsync(Ct),
            };
        });

    private sealed record Sums(decimal PriceListItems, decimal ContractPrices, decimal OrderLineUnitPrices, decimal OnHand, decimal Allocated, decimal OnOrder);
}
