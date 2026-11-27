using System.Text.Json;
using SalesMcp.Domain.Catalog;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Tools;

/// <summary><c>check_stock</c> against the seeded Acme stock fixtures.</summary>
public sealed class CheckStockToolTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // The server seeds its database for the real today, so dates are checked against the local day.
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    private static readonly string _inStock = SkuOf(_fixtures.Offerings.InStock);
    private static readonly string _shortWithReceipt = SkuOf(_fixtures.Offerings.ShortWithReceipt);
    private static readonly string _shortWithoutReceipt = SkuOf(_fixtures.Offerings.ShortWithoutReceipt);

    private static Offering Seeded(Domain.Common.OfferingId id) => McpServerFixture.Seeded.Offerings.Single(o => o.Id == id);

    private static string SkuOf(Domain.Common.OfferingId id) => Seeded(id).Sku.Value;

    private Task<StockResult> Check(string offering, decimal? quantity = null) =>
        server.CallToolAsync<StockResult>("check_stock", quantity is null ? new { offering } : new { offering, quantity });

    [Fact]
    public async Task An_in_stock_product_ships_now_from_both_warehouses()
    {
        var before = DateTimeOffset.UtcNow;

        var stock = await Check(_inStock);

        Assert.Equal("now", stock.Availability);
        Assert.Equal(Today, stock.AvailableDate);
        Assert.Equal(["DBN", "JHB"], stock.Warehouses.Select(w => w.Code));
        Assert.All(stock.Warehouses, w => Assert.True(w.Available > 0m, $"{w.Code} has nothing available"));
        Assert.Equal(stock.Warehouses.Sum(w => w.Available), stock.TotalAvailable);
        Assert.Equal(_fixtures.Offerings.InStock.ToString(), stock.OfferingId);
        Assert.InRange(stock.AsOf, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task A_short_product_with_stock_on_order_ships_later_on_a_receipt_date()
    {
        var stock = await Check(_shortWithReceipt);

        Assert.Equal("later", stock.Availability);
        Assert.Equal(0m, stock.TotalAvailable);
        Assert.True(stock.AvailableDate > Today, $"expected a future date, got {stock.AvailableDate}");
        Assert.Contains(stock.Warehouses, w => w.NextReceiptDate == stock.AvailableDate);
        Assert.All(stock.Warehouses, w => Assert.True(w.OnOrder > 0m));
    }

    [Fact]
    public async Task A_short_product_with_nothing_on_order_is_unknown()
    {
        var stock = await Check(_shortWithoutReceipt);

        Assert.Equal("unknown", stock.Availability);
        Assert.Null(stock.AvailableDate);
        Assert.All(stock.Warehouses, w =>
        {
            Assert.Equal(0m, w.OnOrder);
            Assert.Null(w.NextReceiptDate);
            Assert.Null(w.AvailableDate);
        });
    }

    [Fact]
    public async Task More_than_is_in_stock_or_on_order_is_unknown()
    {
        var stock = await Check(_inStock, 999_999m);

        Assert.Equal("unknown", stock.Availability);
        Assert.Null(stock.AvailableDate);
    }

    [Fact]
    public async Task The_offering_can_be_given_by_id()
    {
        var stock = await Check(_fixtures.Offerings.InStock.ToString());

        Assert.Equal(_inStock, stock.Sku);
        Assert.Equal(Seeded(_fixtures.Offerings.InStock).Name, stock.Name);
    }

    [Fact]
    public async Task The_result_has_figures_and_no_pressure_language()
    {
        // Guardrail B2: numbers and dates only, whatever the stock position.
        string[] skus = [_inStock, _shortWithReceipt, _shortWithoutReceipt];
        string[] banned = ["only", "hurry", "last", "left", "selling fast", "limited", "low stock", "running out"];

        foreach (var sku in skus)
        {
            var raw = (await server.CallToolAsync<JsonElement>("check_stock", new { offering = sku })).GetRawText();
            var words = raw.ToUpperInvariant();
            Assert.All(banned, b => Assert.DoesNotContain(b.ToUpperInvariant(), words, StringComparison.Ordinal));
        }
    }

    [Theory]
    [InlineData("ACM-NOPE-999", "OfferingNotFound")]
    [InlineData("", "OfferingRequired")]
    public async Task Unusable_offerings_fail_with_a_code(string offering, string code) =>
        await server.ExpectToolErrorAsync("check_stock", new { offering }, code);

    [Fact]
    public async Task A_discontinued_product_fails_as_inactive() =>
        await server.ExpectToolErrorAsync("check_stock", new { offering = SkuOf(_fixtures.Offerings.Discontinued) }, "OfferingInactive");

    [Theory]
    [InlineData(0, "QuantityNotPositive")]
    [InlineData(-1, "QuantityNotPositive")]
    [InlineData(2_000_000, "QuantityTooLarge")]
    public async Task Out_of_range_quantities_fail_with_a_code(int quantity, string code) =>
        await server.ExpectToolErrorAsync("check_stock", new { offering = _inStock, quantity }, code);

    [Fact]
    public async Task Is_read_only_and_takes_an_offering_and_optional_quantity()
    {
        var tool = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Single(t => t.Name == "check_stock");

        Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        var required = tool.ProtocolTool.InputSchema.GetProperty("required").EnumerateArray().Select(e => e.GetString());
        Assert.Equal(["offering"], required);
    }
}
