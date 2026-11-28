using SalesMcp.Application.Catalog;
using SalesMcp.Application.Customers;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Pricing;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Application.Tests.Pricing;

public class GetPriceTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly DateTimeOffset _now = new(2026, 11, 26, 8, 30, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private static Product NewProduct(string sku = "ACM-FAS-101", VatCode vat = VatCode.Standard) =>
        Product.Create(OfferingId.New(), _acme, sku, "Hex bolts M10", "", vat, UnitOfMeasure.Box, 3, 30).Value;

    private static Customer NewCustomer(PriceListId? list = null, BusinessId? business = null) =>
        Customer.Create(
            CustomerId.New(), business ?? _acme, "KES-001", "Kestrel Ridge", null, Zar(50_000m), Zar(0m),
            PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30).Value, list,
            allowBackOrders: true, allowPartialShipment: true).Value;

    private static PriceList DefaultList(Product product, string name = "Acme list prices")
    {
        var list = PriceList.Create(PriceListId.New(), _acme, name, "ZAR", isDefault: true).Value;
        list.AddItem(product.Id, new Quantity(1m), Zar(100m));
        list.AddItem(product.Id, new Quantity(10m), Zar(95m));
        return list;
    }

    private static GetPrice Pricer(Offering offering, Customer customer, PriceSources? sources) =>
        new(
            new OfferingLookup(new FakeCatalog(offering)),
            new FakeCustomers(customer),
            new FakePricing(sources),
            new PriceResolver(VatRateTable.SouthAfrica),
            new FixedTime(_now));

    private static Task<Result<PricedLine>> Price(GetPrice pricer, Customer customer, string offering, decimal quantity) =>
        pricer.ExecuteAsync(_acme, customer.Id, offering, quantity, Ct);

    [Fact]
    public async Task Prices_from_the_default_list_with_vat_and_as_of()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();

        var line = (await Price(Pricer(bolts, customer, new(DefaultList(bolts), null, [])), customer, "ACM-FAS-101", 3m)).Value;

        Assert.Equal(PriceStepKind.DefaultPriceList, line.Breakdown.Source);
        Assert.Equal(Zar(100m), line.Breakdown.UnitPrice);
        Assert.Equal(Zar(300m), line.Breakdown.Net);
        Assert.Equal(Zar(45m), line.Breakdown.Vat.Vat);
        Assert.Equal(Zar(345m), line.Breakdown.Total);
        Assert.Equal(_now, line.Breakdown.AsOf);
        Assert.Equal(UnitOfMeasure.Box, line.Unit);
    }

    [Fact]
    public async Task A_quantity_break_lowers_the_unit_price()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();
        var pricer = Pricer(bolts, customer, new(DefaultList(bolts), null, []));

        Assert.Equal(Zar(100m), (await Price(pricer, customer, "ACM-FAS-101", 9m)).Value.Breakdown.UnitPrice);
        Assert.Equal(Zar(95m), (await Price(pricer, customer, "ACM-FAS-101", 10m)).Value.Breakdown.UnitPrice);
    }

    [Fact]
    public async Task A_valid_contract_beats_the_list()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();
        var contract = ContractPrice.Create(customer.Id, bolts.Id, Zar(80m), new(2026, 1, 1), new(2026, 12, 31)).Value;

        var line = (await Price(Pricer(bolts, customer, new(DefaultList(bolts), null, [contract])), customer, "ACM-FAS-101", 1m)).Value;

        Assert.Equal(PriceStepKind.ContractPrice, line.Breakdown.Source);
        Assert.Equal(Zar(80m), line.Breakdown.UnitPrice);
    }

    [Fact]
    public async Task No_price_list_at_all_is_no_price_not_zero()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();

        Assert.Equal(PricingErrors.NoPrice, (await Price(Pricer(bolts, customer, null), customer, "ACM-FAS-101", 1m)).Error);
    }

    [Fact]
    public async Task An_unlisted_offering_is_no_price_not_zero()
    {
        var bolts = NewProduct();
        var other = NewProduct("ACM-FAS-999");
        var customer = NewCustomer();

        Assert.Equal(PricingErrors.NoPrice, (await Price(Pricer(bolts, customer, new(DefaultList(other), null, [])), customer, "ACM-FAS-101", 1m)).Error);
    }

    [Theory]
    [InlineData(0, "QuantityNotPositive")]
    [InlineData(-5, "QuantityNotPositive")]
    [InlineData(1_000_001, "QuantityTooLarge")]
    public async Task Out_of_range_quantities_are_refused(int quantity, string code)
    {
        var bolts = NewProduct();
        var customer = NewCustomer();

        Assert.Equal(code, (await Price(Pricer(bolts, customer, new(DefaultList(bolts), null, [])), customer, "ACM-FAS-101", quantity)).Error.Code);
    }

    [Fact]
    public async Task A_discontinued_offering_is_refused()
    {
        var bolts = NewProduct();
        bolts.Deactivate();
        var customer = NewCustomer();

        Assert.Equal(OfferingLookupErrors.OfferingInactive, (await Price(Pricer(bolts, customer, new(DefaultList(bolts), null, [])), customer, "ACM-FAS-101", 1m)).Error);
    }

    [Fact]
    public async Task A_customer_from_another_business_is_not_found()
    {
        var bolts = NewProduct();
        var stranger = NewCustomer(business: BusinessId.New());

        Assert.Equal(CustomerQueryErrors.CustomerNotFound, (await Price(Pricer(bolts, stranger, new(DefaultList(bolts), null, [])), stranger, "ACM-FAS-101", 1m)).Error);
    }

    [Fact]
    public async Task Step_details_are_cleaned_as_untrusted_text()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();

        var line = (await Price(Pricer(bolts, customer, new(DefaultList(bolts, "Acme​ list‮ prices"), null, [])), customer, "ACM-FAS-101", 1m)).Value;

        Assert.Equal("Acme list prices, 1+ break", line.Steps[0].Detail);
    }

    [Fact]
    public async Task No_discount_is_ever_applied()
    {
        var bolts = NewProduct();
        var customer = NewCustomer();

        var line = (await Price(Pricer(bolts, customer, new(DefaultList(bolts), null, [])), customer, "ACM-FAS-101", 10m)).Value;

        Assert.Equal(Percentage.Zero, line.Breakdown.DiscountRate);
        Assert.True(line.Breakdown.Discount.IsZero);
        Assert.DoesNotContain(line.Steps, s => s.Kind == PriceStepKind.Discount);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeCatalog(Offering offering) : ICatalogReader
    {
        public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
            Task.FromResult(offering.Id == id ? offering : null);

        public Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Offering>>([offering]);

        public Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken) =>
            Task.FromResult(offering.BusinessId == businessId && offering.Sku == sku ? offering : null);

        public Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult(new OfferingRuleSet());
    }

    private sealed class FakeCustomers(Customer customer) : ICustomerReader
    {
        public Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(customer.Id == id ? customer : null);

        public Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Contact>>([]);

        public Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>([customer]);
    }

    private sealed class FakePricing(PriceSources? sources) : IPricingReader
    {
        public Task<PriceSources?> GetPriceSourcesAsync(Customer customer, CancellationToken cancellationToken) =>
            Task.FromResult(sources);
    }
}
