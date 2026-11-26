using System.Text.Json;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Infrastructure.Seeding;
using SalesMcp.IntegrationTests.Harness;
using SalesMcp.Server.Stdio.Tools;

namespace SalesMcp.IntegrationTests.Tools;

/// <summary><c>get_customer</c> and <c>get_customer_credit</c> for the top Acme customer.</summary>
public sealed class CustomerToolTests(McpServerFixture server) : IClassFixture<McpServerFixture>
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    internal static Customer Seeded(CustomerId id) => McpServerFixture.Seeded.Customers.Single(c => c.Id == id);

    [Fact]
    public async Task Get_customer_describes_the_bound_customer()
    {
        var top = Seeded(_fixtures.Customers.Top);

        var customer = await server.CallToolAsync<CustomerResult>("get_customer");

        Assert.Equal(top.AccountNumber.Value, customer.AccountNumber);
        Assert.Equal(top.Name, customer.Name);
        Assert.Equal(Wire.Name(top.PaymentTerms.Basis), customer.PaymentTerms.Basis);
        Assert.Equal(top.PaymentTerms.Days, customer.PaymentTerms.Days);
        Assert.Equal(top.PaymentTerms.ToString(), customer.PaymentTerms.Description);
        Assert.Equal(top.AllowBackOrders, customer.AllowBackOrders);
        Assert.Equal(top.AllowPartialShipment, customer.AllowPartialShipment);
    }

    [Fact]
    public async Task Get_customer_returns_no_credit_figures()
    {
        var raw = await server.CallToolAsync<JsonElement>("get_customer");

        Assert.Equal(
            ["accountNumber", "allowBackOrders", "allowPartialShipment", "name", "paymentTerms"],
            raw.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Get_customer_credit_reports_the_seeded_position_with_as_of()
    {
        var top = Seeded(_fixtures.Customers.Top);
        var before = DateTimeOffset.UtcNow;

        var credit = await server.CallToolAsync<CustomerCreditResult>("get_customer_credit");

        Assert.Equal(new MoneyAmount(top.CreditLimit.Round().Amount, "ZAR"), credit.CreditLimit);
        Assert.Equal(new MoneyAmount(top.Balance.Round().Amount, "ZAR"), credit.Balance);
        Assert.Equal((top.CreditLimit - top.Balance).Round().Amount, credit.AvailableCredit.Amount);
        Assert.False(credit.IsOverLimit);
        Assert.False(credit.OnHold);
        Assert.InRange(credit.AsOf, before.AddSeconds(-1), DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("get_customer")]
    [InlineData("get_customer_credit")]
    public async Task A_smuggled_customer_argument_never_changes_whose_data_comes_back(string tool)
    {
        // Guardrail D2: the model can't pick the customer. Extra arguments don't reach the tool.
        var other = Seeded(_fixtures.Customers.OnHold);
        var arguments = new { customerId = other.Id.Value, accountNumber = other.AccountNumber.Value, customer = other.AccountNumber.Value };

        var smuggled = await server.CallToolAsync<JsonElement>(tool, arguments);
        var plain = await server.CallToolAsync<JsonElement>(tool);

        Assert.Equal(Strip(plain), Strip(smuggled));
        Assert.DoesNotContain(other.AccountNumber.Value, smuggled.GetRawText(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("get_customer")]
    [InlineData("get_customer_credit")]
    public async Task Both_tools_are_read_only_and_take_no_arguments(string name)
    {
        var tool = (await server.Client.ListToolsAsync(cancellationToken: Ct)).Single(t => t.Name == name);

        Assert.True(tool.ProtocolTool.Annotations?.ReadOnlyHint);
        Assert.False(tool.ProtocolTool.Annotations?.OpenWorldHint);
        Assert.False(
            tool.ProtocolTool.InputSchema.TryGetProperty("properties", out var properties) && properties.EnumerateObject().Any(),
            $"{name} should take no arguments: {tool.ProtocolTool.InputSchema}");
    }

    // asOf differs between two calls, so compare everything else.
    private static string Strip(JsonElement result) =>
        JsonSerializer.Serialize(result.EnumerateObject().Where(p => p.Name != "asOf").ToDictionary(p => p.Name, p => p.Value));
}

/// <summary>The credit tool for customers in each seeded credit situation.</summary>
public sealed class CustomerCreditCaseTests
{
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    public sealed class OnHoldServer : McpServerFixture
    {
        protected override string CustomerAccount => CustomerToolTests.Seeded(_fixtures.Customers.OnHold).AccountNumber.Value;
    }

    public sealed class NearLimitServer : McpServerFixture
    {
        protected override string CustomerAccount => CustomerToolTests.Seeded(_fixtures.Customers.NearCreditLimit).AccountNumber.Value;
    }

    public sealed class CashOnlyServer : McpServerFixture
    {
        protected override string CustomerAccount => CustomerToolTests.Seeded(_fixtures.Customers.CashOnly).AccountNumber.Value;
    }

    public sealed class OnHold(OnHoldServer server) : IClassFixture<OnHoldServer>
    {
        [Fact]
        public async Task Reports_the_hold() =>
            Assert.True((await server.CallToolAsync<CustomerCreditResult>("get_customer_credit")).OnHold);
    }

    public sealed class NearLimit(NearLimitServer server) : IClassFixture<NearLimitServer>
    {
        [Fact]
        public async Task Shows_little_credit_left()
        {
            var customer = CustomerToolTests.Seeded(_fixtures.Customers.NearCreditLimit);

            var credit = await server.CallToolAsync<CustomerCreditResult>("get_customer_credit");

            Assert.InRange(credit.AvailableCredit.Amount, 0m, customer.CreditLimit.Amount * 0.1m);
        }
    }

    public sealed class CashOnly(CashOnlyServer server) : IClassFixture<CashOnlyServer>
    {
        [Fact]
        public async Task Pays_cash_with_order()
        {
            var customer = await server.CallToolAsync<CustomerResult>("get_customer");

            Assert.Equal("cashWithOrder", customer.PaymentTerms.Basis);
            Assert.Equal(0, customer.PaymentTerms.Days);
        }
    }
}
