using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;
using SalesMcp.Infrastructure.Persistence;

namespace SalesMcp.IntegrationTests.Persistence;

public sealed class PersistenceTests : IAsyncLifetime
{
    private static readonly DateOnly _today = new(2026, 11, 19);
    private static readonly DateTimeOffset _now = new(2026, 11, 19, 9, 0, 0, TimeSpan.FromHours(2));

    private readonly Business _acme = Business.Create(BusinessId.New(), "Acme Distributors", "ZAR", "en-ZA").Value;
    private SqliteDatabase _db = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _db = await SqliteDatabase.CreateAsync();
        await _db.SaveAsync(_acme);
    }

    public ValueTask DisposeAsync() => _db.DisposeAsync();

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private Product NewProduct(string sku, VatCode vatCode = VatCode.Standard) =>
        Product.Create(OfferingId.New(), _acme.Id, sku, sku + " name", "Sealed both sides.", vatCode, UnitOfMeasure.Box, 5, 30).Value;

    private Customer NewCustomer(string account, PriceListId? priceList = null) =>
        Customer.Create(
            CustomerId.New(), _acme.Id, account, "Builders Warehouse " + account, "4123456789",
            Zar(50_000m), Zar(-125.5m), PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30).Value,
            priceList, allowBackOrders: true, allowPartialShipment: false).Value;

    private static T Get<T>(IServiceProvider sp)
        where T : notnull => sp.GetRequiredService<T>();

    [Fact]
    public Task The_migration_matches_the_model() =>
        _db.WithContextAsync(db =>
        {
            Assert.Empty(db.Database.GetPendingMigrations());
            Assert.False(db.Database.HasPendingModelChanges(), "Run `dotnet ef migrations add` – the model has changed.");
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Round_trips_all_three_kinds_of_offering()
    {
        var product = NewProduct("ACM-BRG-6204");
        var service = Service.Create(OfferingId.New(), _acme.Id, "ACM-SVC-INST", "Install", "", VatCode.Standard, ServicePricingModel.NotToExceed).Value;
        var plan = Plan.Create(OfferingId.New(), _acme.Id, "ACM-PLN-SUP", "Support", "", VatCode.Exempt, BillingPeriod.Annual).Value;
        await _db.SaveAsync(product, service, plan);

        var loaded = await _db.InScopeAsync(async sp => (
            Product: (Product)(await Get<ICatalogReader>(sp).GetOfferingAsync(product.Id, Ct))!,
            Service: (Service)(await Get<ICatalogReader>(sp).GetOfferingAsync(service.Id, Ct))!,
            Plan: (Plan)(await Get<ICatalogReader>(sp).GetOfferingAsync(plan.Id, Ct))!));

        Assert.Equal(product.Sku, loaded.Product.Sku);
        Assert.Equal(product.Name, loaded.Product.Name);
        Assert.Equal(product.Description, loaded.Product.Description);
        Assert.Equal(_acme.Id, loaded.Product.BusinessId);
        Assert.Equal(VatCode.Standard, loaded.Product.VatCode);
        Assert.Equal(UnitOfMeasure.Box, loaded.Product.Unit);
        Assert.Equal(5, loaded.Product.LeadTimeDays);
        Assert.Equal(30, loaded.Product.ReturnWindowDays);
        Assert.True(loaded.Product.IsActive);
        Assert.Equal(ServicePricingModel.NotToExceed, loaded.Service.PricingModel);
        Assert.Equal(BillingPeriod.Annual, loaded.Plan.BillingPeriod);
        Assert.Equal(VatCode.Exempt, loaded.Plan.VatCode);
    }

    [Fact]
    public async Task Lists_active_offerings_by_sku()
    {
        var b = NewProduct("ACM-B");
        var a = NewProduct("ACM-A");
        var gone = NewProduct("ACM-C");
        gone.Deactivate();
        await _db.SaveAsync(b, a, gone);

        var skus = await _db.InScopeAsync(async sp =>
            (await Get<ICatalogReader>(sp).ListActiveOfferingsAsync(_acme.Id, Ct)).Select(o => o.Sku.Value).ToList());

        Assert.Equal(["ACM-A", "ACM-B"], skus);
    }

    [Fact]
    public async Task Round_trips_offering_rules_as_a_rule_set()
    {
        var server = NewProduct("ACM-SRV");
        var rails = NewProduct("ACM-RAIL");
        await _db.SaveAsync(server, rails, OfferingRule.Create(server.Id, OfferingRuleKind.Requires, rails.Id, "Needs rails.").Value);

        var rules = await _db.InScopeAsync(sp => Get<ICatalogReader>(sp).GetRulesAsync(_acme.Id, Ct));

        Assert.Equal([rails.Id], rules.RequiredBy(server.Id));
        Assert.Equal("Needs rails.", Assert.Single(rules.Rules).Reason);
    }

    [Fact]
    public async Task Round_trips_customers_and_contacts_with_consent()
    {
        var customer = NewCustomer("BLD-0042");
        customer.PlaceOnHold();
        var contact = Contact.Create(ContactId.New(), customer.Id, "Thandi Mokoena", "thandi@example.co.za").Value;
        contact.RecordMarketingConsent(MarketingConsent.OptIn(_now, ConsentSource.WebForm).Value);
        var neverAsked = Contact.Create(ContactId.New(), customer.Id, "Sipho Dlamini", null).Value;
        await _db.SaveAsync(customer, contact, neverAsked);

        var (loaded, contacts) = await _db.InScopeAsync(async sp => (
            (await Get<ICustomerReader>(sp).GetCustomerAsync(customer.Id, Ct))!,
            await Get<ICustomerReader>(sp).ListContactsAsync(customer.Id, Ct)));

        Assert.Equal(customer.AccountNumber, loaded.AccountNumber);
        Assert.Equal(customer.Name, loaded.Name);
        Assert.Equal(customer.VatNumber, loaded.VatNumber);
        Assert.Equal(Zar(50_000m), loaded.CreditLimit);
        Assert.Equal(Zar(-125.5m), loaded.Balance);
        Assert.Equal(customer.PaymentTerms, loaded.PaymentTerms);
        Assert.Null(loaded.PriceListId);
        Assert.True(loaded.AllowBackOrders);
        Assert.False(loaded.AllowPartialShipment);
        Assert.True(loaded.OnHold);

        Assert.Equal(["Sipho Dlamini", "Thandi Mokoena"], contacts.Select(c => c.Name));
        Assert.Null(contacts[0].MarketingConsent);
        Assert.Null(contacts[0].Email);
        Assert.True(contacts[1].MayReceiveMarketing);
        Assert.Equal(_now, contacts[1].MarketingConsent!.RecordedAt);
        Assert.Equal(ConsentSource.WebForm, contacts[1].MarketingConsent!.Source);
    }

    [Fact]
    public async Task Loads_price_sources_that_price_the_same_as_in_memory()
    {
        var bearing = NewProduct("ACM-BRG-6204");
        var listPrices = PriceList.Create(PriceListId.New(), _acme.Id, "List 2026", "ZAR", isDefault: true).Value;
        listPrices.AddItem(bearing.Id, new Quantity(1), Zar(100m));
        listPrices.AddItem(bearing.Id, new Quantity(10), Zar(90m));
        var trade = PriceList.Create(PriceListId.New(), _acme.Id, "Trade 2026", "ZAR").Value;
        trade.AddItem(bearing.Id, new Quantity(10), Zar(85m));
        var customer = NewCustomer("BLD-0042", trade.Id);
        var contract = ContractPrice.Create(customer.Id, bearing.Id, Zar(70.125m), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31)).Value;
        await _db.SaveAsync(bearing, listPrices, trade, customer, contract);

        var sources = (await _db.InScopeAsync(sp => Get<IPricingReader>(sp).GetPriceSourcesAsync(customer, Ct)))!;

        Assert.Equal(listPrices.Id, sources.DefaultList.Id);
        Assert.True(sources.DefaultList.IsDefault);
        Assert.Equal(2, sources.DefaultList.Items.Count);
        Assert.Equal(trade.Id, sources.CustomerList!.Id);
        Assert.Equal(Zar(70.125m), Assert.Single(sources.Contracts).UnitPrice);

        var resolver = new PriceResolver(VatRateTable.SouthAfrica);
        var request = new PriceRequest(bearing, customer, new Quantity(12), Percentage.Zero, _now);
        var fromDb = resolver.Resolve(request, sources).Value;
        var inMemory = resolver.Resolve(request, new PriceSources(listPrices, trade, [contract])).Value;

        Assert.Equal(inMemory.UnitPrice, fromDb.UnitPrice);
        Assert.Equal(inMemory.Total, fromDb.Total);
        Assert.Equal(PriceStepKind.ContractPrice, fromDb.Source);
    }

    [Fact]
    public async Task No_price_sources_without_a_default_list()
    {
        var customer = NewCustomer("BLD-0042");
        await _db.SaveAsync(customer);

        Assert.Null(await _db.InScopeAsync(sp => Get<IPricingReader>(sp).GetPriceSourcesAsync(customer, Ct)));
    }

    [Fact]
    public async Task A_business_can_have_only_one_default_list()
    {
        await _db.SaveAsync(PriceList.Create(PriceListId.New(), _acme.Id, "List A", "ZAR", isDefault: true).Value);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            _db.SaveAsync(PriceList.Create(PriceListId.New(), _acme.Id, "List B", "ZAR", isDefault: true).Value));
        await _db.SaveAsync(PriceList.Create(PriceListId.New(), _acme.Id, "Trade", "ZAR").Value);
    }

    [Fact]
    public async Task Round_trips_stock_levels()
    {
        var bearing = NewProduct("ACM-BRG-6204");
        var durban = Warehouse.Create(WarehouseId.New(), _acme.Id, "DBN", "Durban").Value;
        var jhb = Warehouse.Create(WarehouseId.New(), _acme.Id, "JHB", "Johannesburg").Value;
        await _db.SaveAsync(
            bearing, durban, jhb,
            StockLevel.Create(bearing.Id, durban.Id, 120.5m, 20m, 200m, new DateOnly(2026, 11, 25)).Value,
            StockLevel.Create(bearing.Id, jhb.Id, 0m, 0m, 0m, null).Value);

        var stock = await _db.InScopeAsync(sp => Get<IStockReader>(sp).GetStockAsync(bearing.Id, Ct));

        var dbn = stock.Single(s => s.WarehouseId == durban.Id);
        Assert.Equal(120.5m, dbn.OnHand);
        Assert.Equal(20m, dbn.Allocated);
        Assert.Equal(200m, dbn.OnOrder);
        Assert.Equal(new DateOnly(2026, 11, 25), dbn.NextReceiptDate);
        Assert.Null(stock.Single(s => s.WarehouseId == jhb.Id).NextReceiptDate);
    }

    [Fact]
    public async Task Round_trips_a_quote_with_its_price_snapshot_and_converts_it_to_an_order()
    {
        var bearing = NewProduct("ACM-BRG-6204");
        var bread = NewProduct("ACM-CAN-BRD", VatCode.ZeroRated);
        var listPrices = PriceList.Create(PriceListId.New(), _acme.Id, "List 2026", "ZAR", isDefault: true).Value;
        listPrices.AddItem(bearing.Id, new Quantity(1), Zar(33.335m));
        listPrices.AddItem(bread.Id, new Quantity(1), Zar(18.99m));
        var customer = NewCustomer("BLD-0042");
        await _db.SaveAsync(bearing, bread, listPrices, customer);

        var resolver = new PriceResolver(VatRateTable.SouthAfrica);
        var sources = new PriceSources(listPrices, null, []);
        PricedOffering Priced(Offering o, decimal qty, decimal discount) =>
            new(o, resolver.Resolve(new PriceRequest(o, customer, new Quantity(qty), new Percentage(discount), _now), sources).Value);

        var quote = Quote.Create(
            QuoteId.New(), QuoteNumber.From(2026, 1).Value, customer, AgentId.Create("acme-distributor-jolt").Value, _today,
            [Priced(bearing, 3, 12.5m), Priced(bread, 7, 0m)], [RiskReducer.ReturnWindow, RiskReducer.PhasedDelivery]).Value;
        await _db.InScopeAsync(async sp =>
        {
            Get<IQuoteRepository>(sp).Add(quote);
            await Get<IUnitOfWork>(sp).SaveChangesAsync(Ct);
        });

        var loaded = (await _db.InScopeAsync(sp => Get<IQuoteRepository>(sp).GetAsync(quote.Id, Ct)))!;

        Assert.Equal(quote.Number, loaded.Number);
        Assert.Equal(quote.AgentId, loaded.AgentId);
        Assert.Equal(QuoteStatus.Issued, loaded.Status);
        Assert.Equal(quote.IssuedOn, loaded.IssuedOn);
        Assert.Equal(quote.ValidUntil, loaded.ValidUntil);
        Assert.Equal(quote.RiskReducers, loaded.RiskReducers);
        Assert.Equal([1, 2], loaded.Lines.OrderBy(l => l.LineNumber).Select(l => l.LineNumber));

        var original = quote.Lines[0].Price;
        var snapshot = loaded.Lines.Single(l => l.LineNumber == 1).Price;
        Assert.Equal(original with { Steps = [] }, snapshot with { Steps = [] });
        Assert.Equal(original.Steps, snapshot.Steps);
        Assert.Equal(quote.Totals.Total, loaded.Totals.Total);
        Assert.Equal(quote.Lines[0].Sku, loaded.Lines.Single(l => l.LineNumber == 1).Sku);

        // Convert in one scope, as convert_quote_to_order will.
        var orderId = OrderId.New();
        await _db.InScopeAsync(async sp =>
        {
            var tracked = (await Get<IQuoteRepository>(sp).GetAsync(quote.Id, Ct))!;
            Get<IOrderRepository>(sp).Add(SalesOrder.FromQuote(orderId, tracked, _today).Value);
            await Get<IUnitOfWork>(sp).SaveChangesAsync(Ct);
        });

        var (order, accepted) = await _db.InScopeAsync(async sp => (
            (await Get<IOrderRepository>(sp).GetAsync(orderId, Ct))!,
            (await Get<IQuoteRepository>(sp).GetAsync(quote.Id, Ct))!));

        Assert.Equal(QuoteStatus.Accepted, accepted.Status);
        Assert.Equal(quote.Id, order.SourceQuoteId);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(_today, order.OrderedOn);
        Assert.Equal(quote.Totals.Total, order.Totals.Total);
        var line = order.Lines.Single(l => l.LineNumber == 1);
        Assert.Equal(Zar(33.335m), line.UnitPrice);
        Assert.Equal(new Quantity(3), line.Quantity);
        Assert.Equal(original.Vat, line.Vat);
    }

    [Fact]
    public async Task Sorts_and_sums_money_numerically_on_sqlite()
    {
        // SQLite has no decimal type. EF's default (text plus a culture-sensitive collation) breaks on
        // en-ZA, where the decimal separator is a comma, so we store scaled integers. Run under en-ZA so
        // CI on any machine catches a regression.
        var culture = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-ZA");
        try
        {
            await SortAndSumAsync();
        }
        finally
        {
            CultureInfo.CurrentCulture = culture;
        }
    }

    [Fact]
    public async Task Refuses_to_store_an_amount_with_more_than_four_decimals()
    {
        var bearing = NewProduct("ACM-BRG-6204");
        var list = PriceList.Create(PriceListId.New(), _acme.Id, "List 2026", "ZAR", isDefault: true).Value;
        list.AddItem(bearing.Id, new Quantity(1), Zar(33.33333m));

        var error = await Assert.ThrowsAnyAsync<Exception>(() => _db.SaveAsync(bearing, list));

        Assert.Contains("more than 4 decimal places", error.ToString(), StringComparison.Ordinal);
    }

    private async Task SortAndSumAsync()
    {
        var bearing = NewProduct("ACM-BRG-6204");
        var customer = NewCustomer("BLD-0042");
        decimal[] amounts = [9.5m, 100m, 10.25m, 2m, 0.0001m];
        var contracts = amounts.Select((a, i) =>
            ContractPrice.Create(customer.Id, bearing.Id, Zar(a), new DateOnly(2026, 1, 1).AddMonths(i), new DateOnly(2026, 1, 1).AddMonths(i)).Value);
        await _db.SaveAsync([bearing, customer, .. contracts]);

        var (sorted, total, aboveNineSeventyFive) = await _db.InScopeAsync(async sp =>
        {
            var db = Get<SalesDbContext>(sp);
            var query = db.ContractPrices.Where(c => c.CustomerId == customer.Id);
            var sortedAmounts = await query.OrderBy(c => c.UnitPrice.Amount).Select(c => c.UnitPrice.Amount).ToListAsync(Ct);
            var sum = await query.SumAsync(c => c.UnitPrice.Amount, Ct);
            var above = await query.Where(c => c.UnitPrice.Amount > 9.75m).CountAsync(Ct);
            return (sortedAmounts, sum, above);
        });

        Assert.Equal([0.0001m, 2m, 9.5m, 10.25m, 100m], sorted);
        Assert.Equal(121.7501m, total);
        Assert.Equal(2, aboveNineSeventyFive);
    }
}
