using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Tests.Pricing;

public class PriceResolverTests
{
    private static readonly DateTimeOffset _today = new(2026, 11, 18, 10, 0, 0, TimeSpan.FromHours(2));

    private readonly BusinessId _acme = BusinessId.New();
    private readonly PriceResolver _resolver = new(VatRateTable.SouthAfrica);
    private readonly Product _bearing;
    private readonly PriceList _listPrices;
    private readonly PriceList _trade;
    private readonly Customer _tradeCustomer;
    private readonly Customer _cashCustomer;
    private readonly ContractPrice _contract;

    public PriceResolverTests()
    {
        _bearing = NewProduct("ACM-BRG-6204", VatCode.Standard);

        _listPrices = PriceList.Create(PriceListId.New(), _acme, "List 2026", "ZAR").Value;
        Add(_listPrices, _bearing, 1, 100m);
        Add(_listPrices, _bearing, 10, 90m);
        Add(_listPrices, _bearing, 50, 80m);

        _trade = PriceList.Create(PriceListId.New(), _acme, "Trade 2026", "ZAR").Value;
        Add(_trade, _bearing, 10, 85m);

        _tradeCustomer = NewCustomer("BLD-0042", _trade.Id);
        _cashCustomer = NewCustomer("CASH-0001", null);

        _contract = ContractPrice.Create(
            _tradeCustomer.Id, _bearing.Id, Zar(70m), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Value;
    }

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private static void Add(PriceList list, Offering offering, decimal minQuantity, decimal price) =>
        Assert.True(list.AddItem(offering.Id, new Quantity(minQuantity), Zar(price)).IsSuccess);

    private Product NewProduct(string sku, VatCode vatCode) =>
        Product.Create(OfferingId.New(), _acme, sku, sku, "", vatCode, UnitOfMeasure.Each, 7, 30).Value;

    private Customer NewCustomer(string account, PriceListId? priceList) =>
        Customer.Create(
            CustomerId.New(), _acme, account, account, null, Zar(10_000m), Zar(0m),
            PaymentTerms.CashWithOrder, priceList, allowBackOrders: true, allowPartialShipment: true).Value;

    private Result<PriceBreakdown> Price(
        Offering offering,
        Customer customer,
        decimal quantity,
        DateTimeOffset? asOf = null,
        decimal discount = 0m,
        ContractPrice[]? contracts = null,
        PriceList? customerList = null) =>
        _resolver.Resolve(
            new PriceRequest(offering, customer, new Quantity(quantity), new Percentage(discount), asOf ?? _today),
            new PriceSources(
                _listPrices,
                customerList ?? (customer.PriceListId == _trade.Id ? _trade : null),
                contracts ?? [_contract]));

    [Fact]
    public void A_valid_contract_wins_over_every_list()
    {
        var price = Price(_bearing, _tradeCustomer, 50).Value;

        Assert.Equal(PriceStepKind.ContractPrice, price.Source);
        Assert.Equal(Zar(70m), price.UnitPrice);
        Assert.Equal(Zar(3_500m), price.Net);
    }

    [Theory]
    [InlineData(2025, 12, 31)]
    [InlineData(2027, 1, 1)]
    public void An_expired_or_future_contract_is_skipped(int year, int month, int day)
    {
        var price = Price(_bearing, _tradeCustomer, 5, new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero)).Value;

        Assert.Equal(PriceStepKind.DefaultPriceList, price.Source);
        Assert.Equal(Zar(100m), price.UnitPrice);
    }

    [Theory]
    [InlineData(2026, 1, 1)]
    [InlineData(2026, 12, 31)]
    public void Contract_dates_are_inclusive(int year, int month, int day) =>
        Assert.Equal(
            PriceStepKind.ContractPrice,
            Price(_bearing, _tradeCustomer, 1, new DateTimeOffset(year, month, day, 12, 0, 0, TimeSpan.Zero)).Value.Source);

    [Fact]
    public void Contract_validity_uses_the_local_date_of_the_pricing_moment()
    {
        // 01:00 on 1 January in South Africa is still 31 December in UTC. The contract has ended locally,
        // so the local date decides.
        var newYearLocal = new DateTimeOffset(2027, 1, 1, 1, 0, 0, TimeSpan.FromHours(2));

        Assert.Equal(PriceStepKind.DefaultPriceList, Price(_bearing, _tradeCustomer, 1, newYearLocal).Value.Source);
    }

    [Fact]
    public void Another_customers_contract_is_ignored() =>
        Assert.Equal(PriceStepKind.DefaultPriceList, Price(_bearing, _cashCustomer, 1).Value.Source);

    [Fact]
    public void The_lowest_of_overlapping_contracts_applies()
    {
        var cheaper = ContractPrice.Create(
            _tradeCustomer.Id, _bearing.Id, Zar(65m), new DateOnly(2026, 6, 1), new DateOnly(2027, 5, 31)).Value;

        Assert.Equal(Zar(65m), Price(_bearing, _tradeCustomer, 1, contracts: [_contract, cheaper]).Value.UnitPrice);
    }

    // Trade customer, no contract: their own list only has a 10+ break, so below 10 the default list applies.
    // The customer list wins from 10 up even where the default list's 50+ break is cheaper: precedence, not lowest price.
    [Theory]
    [InlineData(1, PriceStepKind.DefaultPriceList, 100)]
    [InlineData(9.999, PriceStepKind.DefaultPriceList, 100)]
    [InlineData(10, PriceStepKind.CustomerPriceList, 85)]
    [InlineData(50, PriceStepKind.CustomerPriceList, 85)]
    public void Customer_list_breaks_come_before_the_default_list(double quantity, PriceStepKind source, int unitPrice)
    {
        var price = Price(_bearing, _tradeCustomer, (decimal)quantity, contracts: []).Value;

        Assert.Equal(source, price.Source);
        Assert.Equal(Zar(unitPrice), price.UnitPrice);
    }

    [Theory]
    [InlineData(1, 100)]
    [InlineData(9.999, 100)]
    [InlineData(10, 90)]
    [InlineData(49, 90)]
    [InlineData(50, 80)]
    [InlineData(1000, 80)]
    public void Default_list_picks_the_best_break(double quantity, int unitPrice)
    {
        var price = Price(_bearing, _cashCustomer, (decimal)quantity).Value;

        Assert.Equal(PriceStepKind.DefaultPriceList, price.Source);
        Assert.Equal(Zar(unitPrice), price.UnitPrice);
    }

    [Fact]
    public void No_price_is_a_failure_never_zero()
    {
        var unpriced = NewProduct("ACM-NEW-1", VatCode.Standard);

        Assert.Equal(PricingErrors.NoPrice, Price(unpriced, _tradeCustomer, 1).Error);
    }

    [Fact]
    public void A_quantity_below_every_break_has_no_price()
    {
        var boxed = NewProduct("ACM-BOX-1", VatCode.Standard);
        Add(_listPrices, boxed, 12, 40m);

        Assert.Equal(PricingErrors.NoPrice, Price(boxed, _cashCustomer, 6).Error);
    }

    [Fact]
    public void An_inactive_offering_is_not_priced()
    {
        _bearing.Deactivate();

        Assert.Equal(PricingErrors.OfferingInactive, Price(_bearing, _tradeCustomer, 1).Error);
    }

    [Fact]
    public void Standard_rated_line_adds_15_percent_vat()
    {
        var price = Price(_bearing, _cashCustomer, 10).Value;

        Assert.Equal(Zar(900m), price.Net);
        Assert.Equal(Zar(135m), price.Vat.Vat);
        Assert.Equal(Zar(1_035m), price.Total);
    }

    [Theory]
    [InlineData(VatCode.ZeroRated)]
    [InlineData(VatCode.Exempt)]
    public void Zero_rated_and_exempt_lines_carry_no_vat(VatCode vatCode)
    {
        var item = NewProduct("ACM-ITEM-1", vatCode);
        Add(_listPrices, item, 1, 250m);

        var price = Price(item, _cashCustomer, 2).Value;

        Assert.Equal(Zar(500m), price.Net);
        Assert.True(price.Vat.Vat.IsZero);
        Assert.Equal(Zar(500m), price.Total);
        Assert.Equal(vatCode == VatCode.Exempt, price.Vat.IsExempt);
    }

    [Fact]
    public void Discount_comes_off_before_vat()
    {
        var price = Price(_bearing, _cashCustomer, 10, discount: 10m).Value;

        Assert.Equal(Zar(900m), price.Gross);
        Assert.Equal(Zar(90m), price.Discount);
        Assert.Equal(Zar(810m), price.Net);
        Assert.Equal(Zar(121.50m), price.Vat.Vat);
        Assert.Equal(Zar(931.50m), price.Total);
    }

    [Fact]
    public void Rounds_each_figure_to_cents()
    {
        var item = NewProduct("ACM-CUT-1", VatCode.Standard);
        Add(_listPrices, item, 1, 33.335m);

        var price = Price(item, _cashCustomer, 3, discount: 12.5m).Value;

        Assert.Equal(Zar(100.01m), price.Gross);   // 100.005 rounds away from zero
        Assert.Equal(Zar(12.50m), price.Discount); // 12.50125
        Assert.Equal(Zar(87.51m), price.Net);
        Assert.Equal(Zar(13.13m), price.Vat.Vat);  // 13.1265
        Assert.Equal(Zar(100.64m), price.Total);
    }

    [Fact]
    public void Records_each_step_in_order_with_the_as_of_time()
    {
        var price = Price(_bearing, _cashCustomer, 10, discount: 10m).Value;

        Assert.Equal(
            [PriceStepKind.DefaultPriceList, PriceStepKind.Discount, PriceStepKind.Vat],
            price.Steps.Select(s => s.Kind));
        Assert.Equal("List 2026, 10+ break", price.Steps[0].Detail);
        Assert.Equal(Zar(90m), price.Steps[0].Amount);
        Assert.Equal("10% off", price.Steps[1].Detail);
        Assert.Equal(Zar(-90m), price.Steps[1].Amount);
        Assert.Equal("VAT 15%", price.Steps[2].Detail);
        Assert.Equal(_today, price.AsOf);
        Assert.Equal(_bearing.Id, price.OfferingId);
        Assert.Equal(_cashCustomer.Id, price.CustomerId);
        Assert.Equal(new Quantity(10), price.Quantity);
        Assert.Equal(new Percentage(10m), price.DiscountRate);
    }

    [Fact]
    public void No_discount_step_when_there_is_no_discount() =>
        Assert.DoesNotContain(Price(_bearing, _cashCustomer, 1).Value.Steps, s => s.Kind == PriceStepKind.Discount);

    [Fact]
    public void Describes_contract_and_non_standard_vat_steps()
    {
        var item = NewProduct("ACM-FOOD-1", VatCode.ZeroRated);
        var deal = ContractPrice.Create(
            _tradeCustomer.Id, item.Id, Zar(10m), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Value;

        var steps = Price(item, _tradeCustomer, 1, contracts: [deal]).Value.Steps;

        Assert.Equal("Contract price, valid 2026-01-01 to 2026-12-31", steps[0].Detail);
        Assert.Equal("VAT zero-rated (0%)", steps[^1].Detail);
    }

    [Fact]
    public void Describes_an_exempt_vat_step()
    {
        var item = NewProduct("ACM-FIN-1", VatCode.Exempt);
        Add(_listPrices, item, 1, 10m);

        Assert.Equal("VAT exempt", Price(item, _cashCustomer, 1).Value.Steps[^1].Detail);
    }

    [Fact]
    public void Rejects_a_customer_from_another_business()
    {
        var stranger = Customer.Create(
            CustomerId.New(), BusinessId.New(), "X-1", "Stranger", null, Zar(0m), Zar(0m),
            PaymentTerms.CashWithOrder, null, false, false).Value;

        Assert.Equal(PricingErrors.BusinessMismatch, Price(_bearing, stranger, 1).Error);
    }

    [Fact]
    public void Rejects_a_customer_list_from_another_business()
    {
        var foreign = PriceList.Create(PriceListId.New(), BusinessId.New(), "Foreign", "ZAR").Value;

        Assert.Equal(PricingErrors.BusinessMismatch, Price(_bearing, _tradeCustomer, 1, customerList: foreign).Error);
    }

    [Fact]
    public void Rejects_a_default_list_from_another_business()
    {
        var foreign = PriceList.Create(PriceListId.New(), BusinessId.New(), "Foreign", "ZAR").Value;
        var request = new PriceRequest(_bearing, _cashCustomer, new Quantity(1), Percentage.Zero, _today);

        Assert.Equal(PricingErrors.BusinessMismatch, _resolver.Resolve(request, new PriceSources(foreign, null, [])).Error);
    }

    [Fact]
    public void Rejects_a_list_that_is_not_the_customers_own()
    {
        var other = PriceList.Create(PriceListId.New(), _acme, "Other trade", "ZAR").Value;

        Assert.Equal(PricingErrors.PriceListMismatch, Price(_bearing, _tradeCustomer, 1, customerList: other).Error);
        Assert.Equal(PricingErrors.PriceListMismatch, Price(_bearing, _cashCustomer, 1, customerList: _trade).Error);
    }

    [Fact]
    public void Rejects_a_missing_customer_list()
    {
        var request = new PriceRequest(_bearing, _tradeCustomer, new Quantity(1), Percentage.Zero, _today);

        Assert.Equal(
            PricingErrors.PriceListMismatch,
            _resolver.Resolve(request, new PriceSources(_listPrices, null, [])).Error);
    }

    [Fact]
    public void Rejects_a_customer_list_in_another_currency()
    {
        var dollars = PriceList.Create(_trade.Id, _acme, "Trade USD", "USD").Value;

        Assert.Equal(PricingErrors.CurrencyMismatch, Price(_bearing, _tradeCustomer, 10, customerList: dollars).Error);
    }

    [Fact]
    public void Rejects_a_contract_in_another_currency_rather_than_skipping_it()
    {
        var dollars = ContractPrice.Create(
            _tradeCustomer.Id, _bearing.Id, new Money(4m, "USD"), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Value;

        Assert.Equal(PricingErrors.CurrencyMismatch, Price(_bearing, _tradeCustomer, 1, contracts: [dollars]).Error);
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        var request = new PriceRequest(_bearing, _cashCustomer, new Quantity(1), Percentage.Zero, _today);

        Assert.Throws<ArgumentNullException>(() => new PriceResolver(null!));
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(null!, new PriceSources(_listPrices, null, [])));
        Assert.Throws<ArgumentNullException>(() => _resolver.Resolve(request, null!));
    }
}
