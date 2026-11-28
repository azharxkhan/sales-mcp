using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Domain.Orders;
using SalesMcp.Infrastructure.Persistence;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Tools;

/// <summary><c>get_order_status</c> for the top Acme customer.</summary>
/// <remarks>
/// Order numbers depend on the day the database was seeded (the year of each order date), so the
/// tests read the orders from the server's own database rather than from the in-memory seed.
/// </remarks>
public sealed class GetOrderStatusToolTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<OrderStatusResult> Status(string orderNumber) =>
        server.CallToolAsync<OrderStatusResult>("get_order_status", new { orderNumber });

    [Fact]
    public async Task Returns_the_status_and_lines_of_the_customers_order()
    {
        var order = await StoredOrderAsync(o => o.CustomerId == _fixtures.Customers.Top);
        var before = DateTimeOffset.UtcNow;

        var status = await Status(order.Number.Value);

        Assert.Equal(order.Number.Value, status.Number);
        Assert.Equal(Wire.Name(order.Status), status.Status);
        Assert.False(string.IsNullOrWhiteSpace(status.StatusDescription));
        Assert.Equal(order.OrderedOn, status.OrderedOn);
        Assert.StartsWith("Q-", status.QuoteNumber, StringComparison.Ordinal);
        Assert.Equal(order.Lines.OrderBy(l => l.LineNumber).Select(l => l.Sku.Value), status.Lines.Select(l => l.Sku));
        Assert.Equal(order.Lines.Sum(l => l.Total.Amount), status.Totals.Total.Amount);
        Assert.InRange(status.AsOf, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task The_number_can_be_given_in_any_case()
    {
        var order = await StoredOrderAsync(o => o.CustomerId == _fixtures.Customers.Top);

        var status = await Status(" " + order.Number.Value.ToLowerInvariant() + " ");

        Assert.Equal(order.Number.Value, status.Number);
    }

    [Fact]
    public async Task Another_customers_order_is_not_found()
    {
        // Guardrail D2: the same answer as a number that doesn't exist, so numbers can't be probed.
        var theirs = await StoredOrderAsync(o => o.CustomerId != _fixtures.Customers.Top);

        var error = await server.ExpectToolErrorAsync("get_order_status", new { orderNumber = theirs.Number.Value }, "OrderNotFound");
        var unknown = await server.ExpectToolErrorAsync("get_order_status", new { orderNumber = "SO-2099-999999" }, "OrderNotFound");

        Assert.Equal(unknown.Text, error.Text);
        Assert.DoesNotContain(theirs.Number.Value, error.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "OrderNumberRequired")]
    [InlineData("12345", "OrderNumberInvalid")]
    [InlineData("Q-2026-000001", "OrderNumberInvalid")]
    public async Task Malformed_numbers_fail_with_a_code(string orderNumber, string code) =>
        await server.ExpectToolErrorAsync("get_order_status", new { orderNumber }, code);

    [Fact]
    public async Task Is_read_only_and_takes_only_an_order_number()
    {
        var tool = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Single(t => t.Name == "get_order_status");
        var schema = tool.ProtocolTool.InputSchema;

        Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        Assert.Equal(["orderNumber"], schema.GetProperty("properties").EnumerateObject().Select(p => p.Name));
        Assert.Equal(["orderNumber"], schema.GetProperty("required").EnumerateArray().Select(e => e.GetString()));
    }

    private async Task<SalesOrder> StoredOrderAsync(Func<SalesOrder, bool> which)
    {
        await using var scope = server.Services.CreateAsyncScope();
        var orders = await scope.ServiceProvider.GetRequiredService<SalesDbContext>().SalesOrders.AsNoTracking().ToListAsync(Ct);
        return orders.Where(which).OrderBy(o => o.Number.Value, StringComparer.Ordinal).First();
    }
}
