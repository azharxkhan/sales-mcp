using SalesMcp.Application.Orders;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Application.Tests.Orders;

public class GetOrderStatusTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly DateOnly _today = new(2026, 11, 28);
    private static readonly DateTimeOffset _now = new(2026, 11, 28, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private static Customer NewCustomer(string account) =>
        Customer.Create(
            CustomerId.New(), _acme, account, "Customer " + account, null, Zar(50_000m), Zar(0m),
            PaymentTerms.CashWithOrder, null, allowBackOrders: true, allowPartialShipment: true).Value;

    private static (SalesOrder Order, Quote Quote, Product Product) NewOrder(Customer customer, string productName = "Hex bolts M10")
    {
        var product = Product.Create(OfferingId.New(), _acme, "ACM-FAS-101", productName, "", VatCode.Standard, UnitOfMeasure.Box, 3, 30).Value;
        var list = PriceList.Create(PriceListId.New(), _acme, "Acme list prices", "ZAR", isDefault: true).Value;
        list.AddItem(product.Id, new Quantity(1m), Zar(100m));
        var price = new PriceResolver(VatRateTable.SouthAfrica)
            .Resolve(new PriceRequest(product, customer, new Quantity(4m), Percentage.Zero, _now), new PriceSources(list, null, []))
            .Value;
        var quote = Quote.Create(
            QuoteId.New(), QuoteNumber.From(2026, 7).Value, Business.Create(_acme, "Acme Distributors", "ZAR", "en-ZA").Value, customer, AgentId.Create("acme-distributor-jolt").Value,
            _today, [new PricedOffering(product, price)], []).Value;
        var order = SalesOrder.FromQuote(OrderId.New(), OrderNumber.From(2026, 12).Value, quote, _today).Value;
        return (order, quote, product);
    }

    private static GetOrderStatus UseCase(SalesOrder order, Quote quote, Product product) =>
        new(new FakeOrders(order, quote), new FakeCatalog(product), new FixedTime(_now));

    [Fact]
    public async Task Returns_the_status_lines_and_source_quote()
    {
        var customer = NewCustomer("KES-001");
        var (order, quote, product) = NewOrder(customer);
        Assert.True(order.Release().IsSuccess);

        var view = (await UseCase(order, quote, product).ExecuteAsync(customer.Id, " so-2026-000012 ", Ct)).Value;

        Assert.Equal(OrderStatus.Released, view.Order.Status);
        Assert.Equal(GetOrderStatus.Describe(OrderStatus.Released), view.StatusDescription);
        Assert.Equal(quote.Number, view.QuoteNumber);
        Assert.Equal(UnitOfMeasure.Box, view.Lines.Single().Unit);
        Assert.Equal(Zar(460m), view.Order.Totals.Total);
        Assert.Equal(_now, view.AsOf);
    }

    [Fact]
    public async Task Another_customers_order_is_not_found()
    {
        var owner = NewCustomer("KES-001");
        var other = NewCustomer("NAI-002");
        var (order, quote, product) = NewOrder(owner);

        var result = await UseCase(order, quote, product).ExecuteAsync(other.Id, order.Number.Value, Ct);

        Assert.Equal(OrderQueryErrors.OrderNotFound, result.Error);
    }

    [Theory]
    [InlineData(null, "OrderNumberRequired")]
    [InlineData("  ", "OrderNumberRequired")]
    [InlineData("Q-2026-000007", "OrderNumberInvalid")]
    [InlineData("SO-2026-000099", "OrderNotFound")]
    public async Task Missing_malformed_and_unknown_numbers_fail_with_a_code(string? number, string code)
    {
        var customer = NewCustomer("KES-001");
        var (order, quote, product) = NewOrder(customer);

        Assert.Equal(code, (await UseCase(order, quote, product).ExecuteAsync(customer.Id, number, Ct)).Error.Code);
    }

    [Fact]
    public async Task Line_names_are_cleaned()
    {
        var customer = NewCustomer("KES-001");
        var (order, quote, product) = NewOrder(customer, "Hex​ bolts‮ M10");

        var view = (await UseCase(order, quote, product).ExecuteAsync(customer.Id, order.Number.Value, Ct)).Value;

        Assert.Equal("Hex bolts M10", view.Lines.Single().Name);
    }

    [Theory]
    [InlineData(OrderStatus.Draft)]
    [InlineData(OrderStatus.Released)]
    [InlineData(OrderStatus.PartiallyShipped)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Invoiced)]
    [InlineData(OrderStatus.Cancelled)]
    public void Every_status_has_words_without_pressure(OrderStatus status)
    {
        var words = GetOrderStatus.Describe(status);

        Assert.DoesNotContain("unknown", words, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("hurry", words, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeOrders(SalesOrder order, Quote quote) : IOrderReader
    {
        // Like the real reader, the customer is part of the lookup.
        public Task<SalesOrder?> FindByNumberAsync(CustomerId customerId, OrderNumber number, CancellationToken cancellationToken) =>
            Task.FromResult(order.Number == number && order.CustomerId == customerId ? order : null);

        public Task<QuoteNumber?> GetQuoteNumberAsync(QuoteId id, CancellationToken cancellationToken) =>
            Task.FromResult<QuoteNumber?>(quote.Id == id ? quote.Number : null);
    }

    private sealed class FakeCatalog(Offering offering) : ICatalogReader
    {
        public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
            Task.FromResult(offering.Id == id ? offering : null);

        public Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Offering>>([offering]);

        public Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken) =>
            Task.FromResult(offering.Sku == sku ? offering : null);

        public Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult(new OfferingRuleSet());
    }
}
