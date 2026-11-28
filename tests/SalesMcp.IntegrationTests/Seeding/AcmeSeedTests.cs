using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;
using SalesMcp.Infrastructure.Seeding;

namespace SalesMcp.IntegrationTests.Seeding;

/// <summary>Checks the generated dataset has the shape issue #48 and the benchmarks ask for.</summary>
public class AcmeSeedTests
{
    private static readonly DateOnly _today = new(2026, 11, 19);
    private static readonly SeedDataset _data = AcmeSeed.Build(_today);
    private static readonly SeedManifest _fixtures = AcmeSeed.Manifest;

    [Fact]
    public void Has_120_products_in_8_categories_with_every_vat_code()
    {
        var products = _data.Offerings.OfType<Product>().ToList();

        Assert.Equal(120, products.Count);
        Assert.Equal(8, products.Select(p => p.Sku.Value[..7]).Distinct().Count());
        Assert.Equal(120, products.Select(p => p.Sku).Distinct().Count());
        Assert.Equal(120, products.Select(p => p.Name).Distinct().Count());
        Assert.Equal(Enum.GetValues<VatCode>(), products.Select(p => p.VatCode).Distinct().Order());
        Assert.All(products, p => Assert.Equal(_fixtures.Business, p.BusinessId));
    }

    [Fact]
    public void Has_about_15_valid_rules_of_every_kind()
    {
        var set = new OfferingRuleSet();
        Assert.All(_data.Rules, r => Assert.True(set.Add(r).IsSuccess));

        Assert.InRange(_data.Rules.Count, 13, 17);
        Assert.Equal(Enum.GetValues<OfferingRuleKind>(), _data.Rules.Select(r => r.Kind).Distinct().Order());
        Assert.Contains(_fixtures.Offerings.Battery, set.RequiredBy(_fixtures.Offerings.DrillBody));

        var ids = _data.Offerings.Select(o => o.Id).ToHashSet();
        Assert.All(_data.Rules, r => Assert.True(ids.Contains(r.OfferingId) && ids.Contains(r.TargetOfferingId)));
    }

    [Fact]
    public void Fills_95_to_98_percent_of_active_stock_positions_with_some_on_order()
    {
        var active = _data.Offerings.Where(o => o.IsActive).Select(o => o.Id).ToHashSet();
        var positions = _data.StockLevels.Where(s => active.Contains(s.OfferingId)).ToList();

        Assert.Equal(2, _data.Warehouses.Count);
        Assert.Equal(_data.Offerings.Count * 2, _data.StockLevels.Count);
        Assert.InRange(positions.Count(s => s.Available > 0) / (double)positions.Count, 0.95, 0.98);
        Assert.InRange(positions.Count(s => s.OnOrder > 0) / (double)positions.Count, 0.10, 0.35);
        Assert.All(_data.StockLevels, s => Assert.True(s.NextReceiptDate is null || s.NextReceiptDate > _today));
    }

    [Fact]
    public void Stock_fixtures_behave_as_named()
    {
        var one = new Quantity(1);
        foreach (var warehouse in new[] { _fixtures.Warehouses.Johannesburg, _fixtures.Warehouses.Durban })
        {
            Assert.Equal(_today, Stock(_fixtures.Offerings.InStock, warehouse).AvailableDate(one, _today));

            var withReceipt = Stock(_fixtures.Offerings.ShortWithReceipt, warehouse);
            Assert.Equal(0m, withReceipt.Available);
            Assert.NotNull(withReceipt.AvailableDate(one, _today));
            Assert.True(withReceipt.AvailableDate(one, _today) > _today);

            Assert.Null(Stock(_fixtures.Offerings.ShortWithoutReceipt, warehouse).AvailableDate(one, _today));
        }

        Assert.False(Offering(_fixtures.Offerings.Discontinued).IsActive);
        Assert.Equal(VatCode.ZeroRated, Offering(_fixtures.Offerings.ZeroRated).VatCode);
        Assert.Equal(VatCode.Exempt, Offering(_fixtures.Offerings.Exempt).VatCode);
    }

    [Fact]
    public void Has_40_customers_with_the_named_credit_and_shipping_traits()
    {
        Assert.Equal(40, _data.Customers.Count);
        Assert.Equal(40, _data.Customers.Select(c => c.AccountNumber).Distinct().Count());

        var onHold = Assert.Single(_data.Customers, c => c.OnHold);
        Assert.Equal(_fixtures.Customers.OnHold, onHold.Id);
        Assert.True(onHold.Balance.Amount > onHold.CreditLimit.Amount);

        var nearLimit = _data.Customers.Where(c => !c.OnHold && c.Balance.Amount >= c.CreditLimit.Amount * 0.9m).ToList();
        Assert.InRange(nearLimit.Count, 2, 5);
        Assert.Contains(nearLimit, c => c.Id == _fixtures.Customers.NearCreditLimit);

        Assert.InRange(_data.Customers.Count(c => !c.AllowBackOrders), 3, 8);
        var strict = Customer(_fixtures.Customers.NoBackOrders);
        Assert.False(strict.AllowBackOrders);
        Assert.False(strict.AllowPartialShipment);

        var cash = Customer(_fixtures.Customers.CashOnly);
        Assert.Equal(PaymentTerms.CashWithOrder, cash.PaymentTerms);
        Assert.Null(cash.VatNumber);
    }

    [Fact]
    public void Revenue_is_pareto_shaped()
    {
        var revenue = _data.Orders
            .Where(o => o.Status == OrderStatus.Invoiced)
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Sum(o => o.Totals.Net.Amount))
            .OrderDescending()
            .ToList();

        var topFifth = revenue.Take(_data.Customers.Count / 5).Sum();
        Assert.InRange(topFifth / revenue.Sum(), 0.60m, 0.90m);
        Assert.Equal(_fixtures.Customers.Top, InvoicedRevenueLeader());
    }

    [Fact]
    public void Prices_come_from_a_default_list_two_customer_lists_and_contracts_for_top_customers()
    {
        var standard = Assert.Single(_data.PriceLists, l => l.IsDefault);
        Assert.Equal(_fixtures.PriceLists.Default, standard.Id);
        Assert.Equal(3, _data.PriceLists.Count);
        Assert.All(_data.Offerings, o => Assert.NotNull(standard.BestBreak(o.Id, new Quantity(1))));

        var breaks = standard.Items.Where(i => i.OfferingId == _fixtures.Offerings.QuantityBreaks).ToList();
        Assert.Equal([1m, 10m, 50m], breaks.Select(b => b.MinQuantity.Value).Order());
        Assert.True(breaks.Single(b => b.MinQuantity.Value == 50m).UnitPrice.Amount < breaks.Single(b => b.MinQuantity.Value == 1m).UnitPrice.Amount);

        Assert.Equal(_fixtures.PriceLists.Mining, Customer(_fixtures.Customers.Top).PriceListId);
        Assert.Contains(_data.Customers, c => c.PriceListId == _fixtures.PriceLists.Trade);

        var withContracts = _data.ContractPrices.Select(c => c.CustomerId).Distinct().ToList();
        Assert.Equal(5, withContracts.Count);
        Assert.Contains(_fixtures.Customers.Top, withContracts);
        Assert.All(_data.ContractPrices, c => Assert.True(c.IsValidOn(_today)));
    }

    [Fact]
    public void Resolves_the_top_customers_contract_price_today()
    {
        var top = Customer(_fixtures.Customers.Top);
        var drill = Offering(_fixtures.Offerings.DrillBody);
        var sources = new PriceSources(
            _data.PriceLists.Single(l => l.IsDefault),
            _data.PriceLists.Single(l => l.Id == top.PriceListId),
            [.. _data.ContractPrices.Where(c => c.CustomerId == top.Id)]);

        var price = new PriceResolver(VatRateTable.SouthAfrica).Resolve(
            new PriceRequest(drill, top, new Quantity(1), Percentage.Zero, new DateTimeOffset(_today.ToDateTime(new TimeOnly(9, 0)), TimeSpan.FromHours(2))),
            sources).Value;

        Assert.Contains(price.Steps, s => s.Kind == PriceStepKind.ContractPrice);
    }

    [Fact]
    public void Wins_about_20_percent_of_quotes_from_new_customers_and_45_from_existing()
    {
        var windowStart = _today.AddDays(-AcmeSeed.HistoryDays);
        var decided = _data.Quotes
            .Where(q => q.IssuedOn >= windowStart && (q.Status != QuoteStatus.Issued || q.IsExpired(_today)))
            .Select(q => (Segment: SegmentWhenIssued(q), Won: q.Status == QuoteStatus.Accepted))
            .ToList();

        var newRate = WinRate(decided, CustomerSegment.New);
        var existingRate = WinRate(decided, CustomerSegment.Existing);

        Assert.InRange(newRate, 0.15, 0.25);
        Assert.InRange(existingRate, 0.40, 0.50);
        Assert.True(decided.Count(d => d.Segment == CustomerSegment.New) >= 25, "Too few new-customer quotes to mean anything.");
    }

    [Fact]
    public void Order_numbers_run_from_1_per_year_of_the_order_date_without_gaps()
    {
        foreach (var year in _data.Orders.GroupBy(o => o.OrderedOn.Year))
        {
            Assert.All(year, o => Assert.StartsWith($"SO-{year.Key}-", o.Number.Value, StringComparison.Ordinal));
            Assert.Equal(
                Enumerable.Range(1, year.Count()),
                year.Select(o => int.Parse(o.Number.Value.AsSpan(8), System.Globalization.CultureInfo.InvariantCulture)).Order());
        }
    }

    [Fact]
    public void Covers_about_12_months_with_open_quotes_and_orders_in_every_stage()
    {
        Assert.InRange(_data.Quotes.Count, 250, 600);
        Assert.True(_data.Quotes.Min(q => q.IssuedOn) <= _today.AddDays(-AcmeSeed.HistoryDays));
        Assert.True(_data.Quotes.Max(q => q.IssuedOn) < _today);
        Assert.Equal(_data.Quotes.Count, _data.Quotes.Select(q => q.Number).Distinct().Count());

        Assert.Contains(_data.Quotes, q => q.Status == QuoteStatus.Issued && !q.IsExpired(_today));
        Assert.Contains(_data.Quotes, q => q.Status == QuoteStatus.Issued && q.IsExpired(_today));
        Assert.Contains(_data.Quotes, q => q.Status == QuoteStatus.Declined);
        Assert.Contains(_data.Quotes, q => q.AgentId == _fixtures.Agent);

        Assert.Equal(
            _data.Quotes.Where(q => q.Status == QuoteStatus.Accepted).Select(q => q.Id.Value).Order(),
            _data.Orders.Select(o => o.SourceQuoteId.Value).Order());
        Assert.All(_data.Orders, o => Assert.True(o.OrderedOn <= _today));
        Assert.All(
            [OrderStatus.Released, OrderStatus.Shipped, OrderStatus.Invoiced],
            status => Assert.Contains(_data.Orders, o => o.Status == status));

        Assert.DoesNotContain(_data.Orders, o => o.CustomerId == _fixtures.Customers.New);
        Assert.Contains(_data.Quotes, q => q.CustomerId == _fixtures.Customers.New);
    }

    [Fact]
    public void Uses_only_invented_contacts_with_reserved_email_domains()
    {
        Assert.InRange(_data.Contacts.Count, 40, 120);
        Assert.All(_data.Contacts, c => Assert.EndsWith(".example", c.Email, StringComparison.Ordinal));
        Assert.All(_data.Customers, c => Assert.Contains(_data.Contacts, contact => contact.CustomerId == c.Id));
        Assert.Contains(_data.Contacts, c => c.MayReceiveMarketing);
        Assert.Contains(_data.Contacts, c => c.MarketingConsent is { OptedIn: false });
        Assert.Contains(_data.Contacts, c => c.MarketingConsent is null);
    }

    [Fact]
    public void Is_the_same_every_time_for_the_same_day()
    {
        var again = AcmeSeed.Build(_today);

        Assert.Equal(_data.Quotes.Select(Fingerprint), again.Quotes.Select(Fingerprint));
        Assert.Equal(_data.StockLevels.Select(s => (s.OfferingId, s.OnHand, s.OnOrder)), again.StockLevels.Select(s => (s.OfferingId, s.OnHand, s.OnOrder)));
        Assert.Equal(_data.Customers.Select(c => (c.Id, c.Balance)), again.Customers.Select(c => (c.Id, c.Balance)));

        static string Fingerprint(Quote q) => $"{q.Id} {q.Number} {q.CustomerId} {q.Status} {q.Totals.Total}";
    }

    [Fact]
    public void Keeps_fixture_ids_stable_whatever_the_day()
    {
        var later = AcmeSeed.Build(_today.AddDays(40));

        Assert.Equal(_data.Offerings.Select(o => o.Id), later.Offerings.Select(o => o.Id));
        Assert.Equal(_data.Customers.Select(c => c.Id), later.Customers.Select(c => c.Id));
        Assert.Equal(_data.Contacts.Select(c => c.Id), later.Contacts.Select(c => c.Id));
    }

    private static StockLevel Stock(OfferingId offering, WarehouseId warehouse) =>
        _data.StockLevels.Single(s => s.OfferingId == offering && s.WarehouseId == warehouse);

    private static Offering Offering(OfferingId id) => _data.Offerings.Single(o => o.Id == id);

    private static Customer Customer(CustomerId id) => _data.Customers.Single(c => c.Id == id);

    private static CustomerSegment SegmentWhenIssued(Quote quote) =>
        CustomerSegments.From(_data.Orders.Count(o => o.CustomerId == quote.CustomerId && o.OrderedOn < quote.IssuedOn));

    private static double WinRate(List<(CustomerSegment Segment, bool Won)> decided, CustomerSegment segment)
    {
        var inSegment = decided.Where(d => d.Segment == segment).ToList();
        return inSegment.Count(d => d.Won) / (double)inSegment.Count;
    }

    private static CustomerId InvoicedRevenueLeader() =>
        _data.Orders
            .Where(o => o.Status == OrderStatus.Invoiced)
            .GroupBy(o => o.CustomerId)
            .MaxBy(g => g.Sum(o => o.Totals.Net.Amount))!
            .Key;
}
