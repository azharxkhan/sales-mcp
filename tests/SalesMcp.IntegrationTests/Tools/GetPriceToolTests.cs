using System.Text.Json;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Tax;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Tools;

/// <summary>What the seed says a price should be, worked out with the same resolver.</summary>
internal static class SeededPrices
{
    private static readonly SeedDataset _seed = McpServerFixture.Seeded;

    public static Offering Offering(OfferingId id) => _seed.Offerings.Single(o => o.Id == id);

    public static Customer Customer(CustomerId id) => _seed.Customers.Single(c => c.Id == id);

    /// <summary>
    /// The breakdown for this customer today. The server's database was seeded for the real today,
    /// while this dataset is built for <see cref="McpServerFixture.SeedDay"/>: prices are the same,
    /// and contracts run from 200 days before to 165 days after either day, so both are valid now.
    /// </summary>
    public static PriceBreakdown Expected(CustomerId customerId, OfferingId offeringId, decimal quantity)
    {
        var customer = Customer(customerId);
        var sources = new PriceSources(
            _seed.PriceLists.Single(p => p.IsDefault),
            _seed.PriceLists.SingleOrDefault(p => p.Id == customer.PriceListId),
            [.. _seed.ContractPrices.Where(c => c.CustomerId == customerId)]);
        var request = new PriceRequest(Offering(offeringId), customer, new Quantity(quantity), Percentage.Zero, DateTimeOffset.Now);
        return new PriceResolver(VatRateTable.SouthAfrica).Resolve(request, sources).Value;
    }

    public static MoneyAmount Wire(Money money) => MoneyAmount.From(money);
}

/// <summary><c>get_price</c> for the top Acme customer.</summary>
public sealed class GetPriceToolTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string Sku(OfferingId id) => SeededPrices.Offering(id).Sku.Value;

    private Task<PriceResult> Price(string offering, decimal quantity) =>
        server.CallToolAsync<PriceResult>("get_price", new { offering, quantity });

    [Fact]
    public async Task The_top_customer_gets_the_contract_price_on_the_drill()
    {
        var before = DateTimeOffset.UtcNow;
        var expected = SeededPrices.Expected(_fixtures.Customers.Top, _fixtures.Offerings.DrillBody, 2m);

        var price = await Price(Sku(_fixtures.Offerings.DrillBody), 2m);

        Assert.Equal(PriceStepKind.ContractPrice, expected.Source);
        Assert.Equal("contractPrice", price.Source);
        Assert.Equal(SeededPrices.Wire(expected.UnitPrice), price.UnitPrice);
        Assert.Equal(SeededPrices.Wire(expected.Gross), price.Gross);
        Assert.Equal(SeededPrices.Wire(expected.Net), price.Net);
        Assert.Equal(SeededPrices.Wire(expected.Vat.Vat), price.Vat.Amount);
        Assert.Equal(SeededPrices.Wire(expected.Total), price.Total);
        Assert.Equal(15m, price.Vat.RatePercent);
        Assert.Equal(["contractPrice", "vat"], price.Steps.Select(s => s.Kind));
        Assert.InRange(price.AsOf, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task The_offering_can_be_given_by_id()
    {
        var bySku = await Price(Sku(_fixtures.Offerings.DrillBody), 1m);
        var byId = await Price(_fixtures.Offerings.DrillBody.ToString(), 1m);

        Assert.Equal(bySku.UnitPrice, byId.UnitPrice);
        Assert.Equal(bySku.Sku, byId.Sku);
    }

    [Fact]
    public async Task Exempt_items_carry_no_vat()
    {
        var price = await Price(Sku(_fixtures.Offerings.Exempt), 1m);

        Assert.Equal("exempt", price.Vat.Code);
        Assert.Null(price.Vat.RatePercent);
        Assert.Equal(0m, price.Vat.Amount.Amount);
        Assert.Equal(price.Net, price.Total);
    }

    [Theory]
    [InlineData(0, "QuantityNotPositive")]
    [InlineData(-4, "QuantityNotPositive")]
    [InlineData(5_000_000, "QuantityTooLarge")]
    public async Task Negative_zero_and_huge_quantities_are_rejected(int quantity, string code) =>
        await server.ExpectToolErrorAsync("get_price", new { offering = Sku(_fixtures.Offerings.DrillBody), quantity }, code);

    [Fact]
    public async Task Unknown_and_discontinued_offerings_fail_with_a_code()
    {
        await server.ExpectToolErrorAsync("get_price", new { offering = "ACM-NOPE-999", quantity = 1 }, "OfferingNotFound");
        var inactive = await server.ExpectToolErrorAsync("get_price", new { offering = Sku(_fixtures.Offerings.Discontinued), quantity = 1 }, "OfferingInactive");
        Assert.DoesNotContain("   at ", inactive.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_price_discount_or_customer_from_the_model_is_ignored()
    {
        // Guardrail A1: the server prices everything. Guardrail D2: the session picks the customer.
        var other = SeededPrices.Customer(_fixtures.Customers.CashOnly);
        var offering = Sku(_fixtures.Offerings.DrillBody);
        var smuggled = new
        {
            offering,
            quantity = 2,
            unitPrice = 1,
            price = 1,
            discount = 50,
            discountPercent = 50,
            customerId = other.Id.ToString(),
            accountNumber = other.AccountNumber.Value,
        };

        var tampered = await server.CallToolAsync<JsonElement>("get_price", smuggled);
        var plain = await server.CallToolAsync<JsonElement>("get_price", new { offering, quantity = 2 });

        Assert.Equal(Strip(plain), Strip(tampered));
    }

    [Fact]
    public async Task Is_read_only_and_takes_only_an_offering_and_a_quantity()
    {
        var tool = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Single(t => t.Name == "get_price");
        var schema = tool.ProtocolTool.InputSchema;

        Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        Assert.Equal(["offering", "quantity"], schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["offering", "quantity"], schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Order(StringComparer.Ordinal));
    }

    // asOf differs between two calls, so compare everything else.
    private static string Strip(JsonElement result) =>
        JsonSerializer.Serialize(result.EnumerateObject().Where(p => p.Name != "asOf").ToDictionary(p => p.Name, p => p.Value));
}

/// <summary><c>get_price</c> for a customer on list prices only, so quantity breaks show.</summary>
public sealed class ListPriceToolTests(ListPriceToolTests.ListOnlyServer server) : IClassFixture<ListPriceToolTests.ListOnlyServer>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    /// <summary>The first seeded customer with no price list of its own and no contracts.</summary>
    public static Customer ListOnly { get; } = McpServerFixture.Seeded.Customers
        .Where(c => c.PriceListId is null && !McpServerFixture.Seeded.ContractPrices.Any(p => p.CustomerId == c.Id))
        .OrderBy(c => c.AccountNumber.Value, StringComparer.Ordinal)
        .First();

    public sealed class ListOnlyServer : McpServerFixture
    {
        protected override string CustomerAccount => ListOnly.AccountNumber.Value;
    }

    [Fact]
    public async Task A_quantity_break_applies_at_10_and_50()
    {
        var sku = SeededPrices.Offering(_fixtures.Offerings.QuantityBreaks).Sku.Value;

        var one = await server.CallToolAsync<PriceResult>("get_price", new { offering = sku, quantity = 1 });
        var ten = await server.CallToolAsync<PriceResult>("get_price", new { offering = sku, quantity = 10 });
        var fifty = await server.CallToolAsync<PriceResult>("get_price", new { offering = sku, quantity = 50 });

        Assert.All([one, ten, fifty], p => Assert.Equal("defaultPriceList", p.Source));
        Assert.True(ten.UnitPrice.Amount < one.UnitPrice.Amount, $"10+ ({ten.UnitPrice.Amount}) should be below 1+ ({one.UnitPrice.Amount})");
        Assert.True(fifty.UnitPrice.Amount < ten.UnitPrice.Amount, $"50+ ({fifty.UnitPrice.Amount}) should be below 10+ ({ten.UnitPrice.Amount})");
        Assert.Equal(SeededPrices.Wire(SeededPrices.Expected(ListOnly.Id, _fixtures.Offerings.QuantityBreaks, 10m).UnitPrice), ten.UnitPrice);
        Assert.Contains("10+ break", ten.Steps[0].DetailText, StringComparison.Ordinal);
    }
}
