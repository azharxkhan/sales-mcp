using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using SalesMcp.Application.Quotes;
using SalesMcp.Domain.Quotes;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.IntegrationTests.Tools;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Resources;

/// <summary>The resources, read as the top Acme customer.</summary>
public sealed class SalesResourcesTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private const string NoLongerOpen = "no longer open for acceptance";

    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Lists_the_resources_and_templates()
    {
        var direct = await server.Client.ListResourcesAsync(cancellationToken: Ct);
        var templates = await server.Client.ListResourceTemplatesAsync(cancellationToken: Ct);

        Assert.Equal(["catalog://current", "customer://current"], direct.Select(r => r.Uri).Order(StringComparer.Ordinal));
        Assert.Equal(
            ["catalog://{business}", "customer://{id}", "offering://{id}", "quote://{id}"],
            templates.Select(t => t.UriTemplate).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task The_catalog_is_categories_and_counts_not_the_catalog()
    {
        var current = await ReadJsonAsync("catalog://current");
        var byId = await ReadJsonAsync($"catalog://{_fixtures.Business}");
        var onSale = McpServerFixture.Seeded.Offerings.Count(o => o.IsActive);

        Assert.Equal(current.GetRawText(), byId.GetRawText());
        Assert.Equal(onSale, current.GetProperty("total").GetInt32());
        var categories = current.GetProperty("categories").EnumerateArray().ToList();
        Assert.Equal(8, categories.Count);
        Assert.Equal(onSale, categories.Sum(c => c.GetProperty("count").GetInt32()));
        Assert.Contains("Fasteners", categories.Select(c => c.GetProperty("name").GetString()));
        Assert.DoesNotContain("ACM-", current.GetRawText(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_offering_has_details_and_no_price()
    {
        var drill = SeededPrices.Offering(_fixtures.Offerings.DrillBody);

        var byId = await ReadJsonAsync($"offering://{drill.Id}");
        var bySku = await ReadJsonAsync($"offering://{drill.Sku.Value}");

        Assert.Equal(byId.GetRawText(), bySku.GetRawText());
        Assert.Equal(drill.Sku.Value, byId.GetProperty("sku").GetString());
        Assert.Equal("product", byId.GetProperty("type").GetString());
        Assert.Equal("Power tools", byId.GetProperty("category").GetString());
        Assert.True(byId.TryGetProperty("descriptionText", out _));
        Assert.DoesNotContain(byId.EnumerateObject(), p => p.Name.Contains("price", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Unknown_and_discontinued_offerings_are_errors()
    {
        await ExpectNotFoundAsync("offering://ACM-NOPE-999", "OfferingNotFound");
        await ExpectNotFoundAsync($"offering://{_fixtures.Offerings.Discontinued}", "OfferingInactive");
    }

    [Fact]
    public async Task The_customer_is_only_ever_the_sessions_customer()
    {
        var current = await ReadJsonAsync("customer://current");
        var byId = await ReadJsonAsync($"customer://{_fixtures.Customers.Top}");

        Assert.Equal(current.GetRawText(), byId.GetRawText());
        Assert.Equal(SeededPrices.Customer(_fixtures.Customers.Top).AccountNumber.Value, current.GetProperty("accountNumber").GetString());
        Assert.False(current.TryGetProperty("creditLimit", out _));

        // Guardrail D2: another customer's id reads the same as one that doesn't exist.
        var other = await ExpectNotFoundAsync($"customer://{_fixtures.Customers.CashOnly}", "CustomerNotFound");
        var unknown = await ExpectNotFoundAsync($"customer://{Guid.NewGuid()}", "CustomerNotFound");
        Assert.Equal(unknown, other);
    }

    [Fact]
    public async Task Another_business_has_no_catalog_here() =>
        await ExpectNotFoundAsync($"catalog://{Guid.NewGuid()}", "CatalogNotFound");

    [Fact]
    public async Task A_new_quote_reads_back_as_the_same_document()
    {
        var sku = SeededPrices.Offering(_fixtures.Offerings.DrillBody).Sku.Value;
        var quote = await server.CallToolAsync<QuoteResult>(
            "create_quote", new { lines = new[] { new { offering = sku, quantity = 2 } }, requestId = "res-" + Guid.NewGuid().ToString("N") });

        var byId = await server.ReadResourceAsync($"quote://{quote.QuoteId}");
        var byNumber = await server.ReadResourceAsync($"quote://{quote.Number}");

        Assert.Equal(quote.DocumentText, byId);
        Assert.Equal(quote.DocumentText, byNumber);
    }

    [Fact]
    public async Task A_closed_quote_says_so_before_the_document()
    {
        var declined = await StoredQuoteAsync(q => q.CustomerId == _fixtures.Customers.Top && q.Status != QuoteStatus.Issued);

        var document = await server.ReadResourceAsync($"quote://{declined.Id}");

        Assert.StartsWith("> **This quote is " + NoLongerOpen, document, StringComparison.Ordinal);
        Assert.Contains($"# Quote {declined.Number}", document, StringComparison.Ordinal);
        Assert.Contains(QuoteDocument.OfferStatement, document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Another_customers_quote_is_not_found_by_id_or_number()
    {
        var theirs = await StoredQuoteAsync(q => q.CustomerId != _fixtures.Customers.Top);

        var byId = await ExpectNotFoundAsync($"quote://{theirs.Id}", "QuoteNotFound");
        var byNumber = await ExpectNotFoundAsync($"quote://{theirs.Number}", "QuoteNotFound");
        var unknown = await ExpectNotFoundAsync($"quote://{Guid.NewGuid()}", "QuoteNotFound");

        Assert.Equal(unknown, byId);
        Assert.Equal(unknown, byNumber);
    }

    private async Task<JsonElement> ReadJsonAsync(string uri) =>
        JsonDocument.Parse(await server.ReadResourceAsync(uri)).RootElement.Clone();

    private async Task<string> ExpectNotFoundAsync(string uri, string code)
    {
        var error = await Assert.ThrowsAsync<McpProtocolException>(() => server.ReadResourceAsync(uri));
        Assert.Equal(McpErrorCode.InvalidParams, error.ErrorCode);
        Assert.Contains(code, error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("   at ", error.Message, StringComparison.Ordinal);
        return error.Message;
    }

    private async Task<Quote> StoredQuoteAsync(Func<Quote, bool> which)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var quotes = await scope.ServiceProvider.GetRequiredService<SalesDbContext>().Quotes.AsNoTracking().ToListAsync(Ct);
        return quotes.Where(which).OrderBy(q => q.Number.Value, StringComparer.Ordinal).First();
    }
}
