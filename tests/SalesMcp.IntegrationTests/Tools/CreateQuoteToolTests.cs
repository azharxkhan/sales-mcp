using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Application.Quotes;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Quotes;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Tools;

/// <summary><c>create_quote</c> for the top Acme customer.</summary>
public sealed class CreateQuoteToolTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string Sku(OfferingId id) => SeededPrices.Offering(id).Sku.Value;

    private static string NewKey() => "test-" + Guid.NewGuid().ToString("N");

    private static object[] DrillAndExempt(decimal drills = 2m) =>
    [
        new { offering = Sku(_fixtures.Offerings.DrillBody), quantity = drills },
        new { offering = Sku(_fixtures.Offerings.Exempt), quantity = 1m },
    ];

    private Task<QuoteResult> Quote(object[] lines, string requestId) =>
        server.CallToolAsync<QuoteResult>("create_quote", new { lines, requestId });

    [Fact]
    public async Task Issues_a_numbered_quote_priced_by_the_server()
    {
        var year = DateTime.Now.Year;
        var before = await HighestSequenceAsync(year);
        var drill = SeededPrices.Expected(_fixtures.Customers.Top, _fixtures.Offerings.DrillBody, 2m);
        var exempt = SeededPrices.Expected(_fixtures.Customers.Top, _fixtures.Offerings.Exempt, 1m);

        var quote = await Quote(DrillAndExempt(), NewKey());

        Assert.Equal(QuoteNumber.From(year, before + 1).Value.Value, quote.Number);
        Assert.Equal("issued", quote.Status);
        Assert.Equal(quote.IssuedOn.AddDays(QuoteErrors.DefaultValidDays), quote.ValidUntil);
        Assert.Equal("acme-distributor-jolt", quote.IssuedBy);
        Assert.Equal([1, 2], quote.Lines.Select(l => l.LineNumber));
        Assert.Equal("contractPrice", quote.Lines[0].Source);
        Assert.Equal(SeededPrices.Wire(drill.UnitPrice), quote.Lines[0].UnitPrice);
        Assert.Equal(SeededPrices.Wire(drill.Total), quote.Lines[0].Total);
        Assert.Equal(SeededPrices.Wire(exempt.Total), quote.Lines[1].Total);
        Assert.Equal(SeededPrices.Wire(drill.Total + exempt.Total), quote.Totals.Total);
        Assert.Equal(["exempt", "standard"], quote.Totals.VatByRate.Select(v => v.Code).Order(StringComparer.Ordinal));
        Assert.Equal(quote.Lines.Sum(l => l.Net.Amount), quote.Totals.Net.Amount);
        Assert.Equal("daysFromEndOfMonth", quote.PaymentTerms.Basis);

        Assert.Contains($"# Quote {quote.Number}", quote.DocumentText, StringComparison.Ordinal);
        Assert.Contains($"**Valid until:** {quote.ValidUntil.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}", quote.DocumentText, StringComparison.Ordinal);
        Assert.Contains(QuoteDocument.OfferStatement, quote.DocumentText, StringComparison.Ordinal);

        var stored = await StoredQuoteAsync(quote.QuoteId);
        Assert.Equal(quote.Number, stored.Number.Value);
        Assert.Equal(drill.UnitPrice, stored.Lines[0].Price.UnitPrice);
        Assert.Equal(_fixtures.Customers.Top, stored.CustomerId);
    }

    [Fact]
    public async Task Two_calls_with_one_request_id_create_one_quote()
    {
        var key = NewKey();
        var before = await CountQuotesAsync();

        var first = await Quote(DrillAndExempt(), key);
        var retry = await Quote(DrillAndExempt(), key);

        Assert.Equal(first.QuoteId, retry.QuoteId);
        Assert.Equal(first.Number, retry.Number);
        Assert.Equal(first.DocumentText, retry.DocumentText);
        Assert.Equal(first.Totals.Total, retry.Totals.Total);
        Assert.Equal(first.Totals.VatByRate, retry.Totals.VatByRate);
        Assert.Equal(first.PricedAt, retry.PricedAt);
        Assert.Equal(before + 1, await CountQuotesAsync());
    }

    [Fact]
    public async Task A_reused_request_id_with_different_lines_conflicts()
    {
        var key = NewKey();
        await Quote(DrillAndExempt(2m), key);
        var before = await CountQuotesAsync();

        var conflict = await server.ExpectToolErrorAsync("create_quote", new { lines = DrillAndExempt(3m), requestId = key }, "IdempotencyConflict");

        Assert.DoesNotContain("   at ", conflict.Text, StringComparison.Ordinal);
        Assert.Equal(before, await CountQuotesAsync());
    }

    [Fact]
    public async Task A_smuggled_price_discount_or_customer_is_ignored()
    {
        // Guardrail A1: the server prices every line. Guardrail D2: the session picks the customer.
        var other = SeededPrices.Customer(_fixtures.Customers.CashOnly);
        var smuggled = new
        {
            lines = new object[]
            {
                new { offering = Sku(_fixtures.Offerings.DrillBody), quantity = 2, unitPrice = 1, price = 1, discountPercent = 90, total = 1 },
            },
            requestId = NewKey(),
            unitPrice = 1,
            discountPercent = 50,
            total = 1,
            customerId = other.Id.ToString(),
            accountNumber = other.AccountNumber.Value,
        };
        var expected = SeededPrices.Expected(_fixtures.Customers.Top, _fixtures.Offerings.DrillBody, 2m);

        var quote = await server.CallToolAsync<QuoteResult>("create_quote", smuggled);
        var stored = await StoredQuoteAsync(quote.QuoteId);

        Assert.Equal(SeededPrices.Wire(expected.UnitPrice), quote.Lines[0].UnitPrice);
        Assert.Equal(0m, quote.Lines[0].DiscountPercent);
        Assert.Equal(expected.UnitPrice, stored.Lines[0].Price.UnitPrice);
        Assert.Equal(expected.Total, stored.Totals.Total);
        Assert.Equal(_fixtures.Customers.Top, stored.CustomerId);
    }

    [Fact]
    public async Task Concurrent_quotes_get_consecutive_numbers()
    {
        var year = DateTime.Now.Year;
        var before = await HighestSequenceAsync(year);

        var quotes = await Task.WhenAll(Enumerable.Range(1, 5).Select(i => Quote(DrillAndExempt(i), NewKey())));

        var sequences = quotes.Select(q => int.Parse(q.Number.AsSpan(7), CultureInfo.InvariantCulture)).Order().ToList();
        Assert.Equal(sequences.Count, sequences.Distinct().Count());
        Assert.Equal(sequences[0] + 4, sequences[^1]);
        Assert.True(sequences[0] > before, $"New numbers ({sequences[0]}…) must follow the last ({before}).");
    }

    [Fact]
    public async Task Bad_input_fails_with_a_code_and_issues_nothing()
    {
        var before = await CountQuotesAsync();
        var drill = Sku(_fixtures.Offerings.DrillBody);

        await server.ExpectToolErrorAsync("create_quote", new { lines = Array.Empty<object>(), requestId = NewKey() }, "QuoteEmpty");
        await server.ExpectToolErrorAsync("create_quote", new { lines = DrillAndExempt(), requestId = "short" }, "RequestIdInvalid");
        await server.ExpectToolErrorAsync("create_quote", new { lines = DrillAndExempt(0m), requestId = NewKey() }, "QuantityNotPositive");
        await server.ExpectToolErrorAsync("create_quote", new { lines = new object[] { new { offering = Sku(_fixtures.Offerings.Discontinued), quantity = 1 } }, requestId = NewKey() }, "OfferingInactive");
        await server.ExpectToolErrorAsync("create_quote", new { lines = new object[] { new { offering = drill, quantity = 1 }, new { offering = drill, quantity = 2 } }, requestId = NewKey() }, "LineDuplicate");

        Assert.Equal(before, await CountQuotesAsync());
    }

    [Fact]
    public async Task Is_a_mutation_that_takes_only_lines_and_a_request_id()
    {
        var tool = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Single(t => t.Name == "create_quote");
        var schema = tool.ProtocolTool.InputSchema;
        var annotations = tool.ProtocolTool.Annotations;

        Assert.False(annotations?.ReadOnlyHint);
        Assert.False(annotations?.DestructiveHint);
        Assert.True(annotations?.IdempotentHint);
        Assert.False(annotations?.OpenWorldHint);
        Assert.Equal(["lines", "requestId"], schema.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
        Assert.Equal(["lines", "requestId"], schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()).Order(StringComparer.Ordinal));

        var line = schema.GetProperty("properties").GetProperty("lines").GetProperty("items");
        Assert.Equal(["offering", "quantity"], line.GetProperty("properties").EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    private async Task<T> WithDbAsync<T>(Func<SalesDbContext, Task<T>> work)
    {
        await using var scope = server.Services.CreateAsyncScope();
        return await work(scope.ServiceProvider.GetRequiredService<SalesDbContext>());
    }

    private Task<int> CountQuotesAsync() =>
        WithDbAsync(db => db.Quotes.CountAsync(q => q.CustomerId == _fixtures.Customers.Top, Ct));

    private Task<Quote> StoredQuoteAsync(string id) =>
        WithDbAsync(db => db.Quotes.AsNoTracking().SingleAsync(q => q.Id == new QuoteId(Guid.Parse(id)), Ct));

    private async Task<int> HighestSequenceAsync(int year)
    {
        var numbers = await WithDbAsync(db => db.Quotes.AsNoTracking().Select(q => q.Number).ToListAsync(Ct));
        var prefix = string.Create(CultureInfo.InvariantCulture, $"Q-{year:D4}-");
        return numbers
            .Where(n => n.Value.StartsWith(prefix, StringComparison.Ordinal))
            .Select(n => int.Parse(n.Value.AsSpan(7), CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();
    }
}
