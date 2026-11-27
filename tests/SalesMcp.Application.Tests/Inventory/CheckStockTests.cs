using SalesMcp.Application.Catalog;
using SalesMcp.Application.Inventory;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Application.Tests.Inventory;

public class CheckStockTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly DateTimeOffset _now = new(2026, 11, 26, 8, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly _today = new(2026, 11, 26);

    private static readonly Warehouse _jhb = Warehouse.Create(WarehouseId.New(), _acme, "JHB", "Johannesburg").Value;
    private static readonly Warehouse _dbn = Warehouse.Create(WarehouseId.New(), _acme, "DBN", "Durban​ centre").Value;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Product NewProduct(string sku = "ACM-PPE-101", BusinessId? business = null) =>
        Product.Create(OfferingId.New(), business ?? _acme, sku, "Safety gloves", "", VatCode.Standard, UnitOfMeasure.Pair, 3, 30).Value;

    private static StockLevel Level(Offering product, Warehouse warehouse, decimal onHand, decimal allocated = 0m, decimal onOrder = 0m, DateOnly? receipt = null) =>
        StockLevel.Create(product.Id, warehouse.Id, onHand, allocated, onOrder, receipt).Value;

    private static CheckStock Check(Offering[] offerings, params StockLevel[] levels) =>
        new(new OfferingLookup(new FakeCatalog(offerings)), new FakeStock(levels, [_jhb, _dbn]), new FixedTime(_now));

    private static async Task<StockCheck> Checked(CheckStock check, string offering, decimal? quantity = null) =>
        (await check.ExecuteAsync(_acme, offering, quantity, Ct)).Value;

    private static async Task<DomainError> Refused(CheckStock check, string? offering, decimal? quantity = null) =>
        (await check.ExecuteAsync(_acme, offering, quantity, Ct)).Error;

    [Fact]
    public async Task Stock_on_hand_ships_now_with_each_warehouse_in_code_order()
    {
        var gloves = NewProduct();
        var check = Check([gloves], Level(gloves, _jhb, 40m, allocated: 5m), Level(gloves, _dbn, 12m));

        var stock = await Checked(check, "ACM-PPE-101", 20m);

        Assert.Equal(StockOutlook.Now, stock.Outlook);
        Assert.Equal(_today, stock.AvailableDate);
        Assert.Equal(47m, stock.TotalAvailable);
        Assert.Equal(20m, stock.Quantity);
        Assert.Equal(UnitOfMeasure.Pair, stock.Unit);
        Assert.Equal(["DBN", "JHB"], stock.Warehouses.Select(w => w.Code));
        Assert.Equal([12m, 35m], stock.Warehouses.Select(w => w.Available));
        Assert.Equal(_now, stock.AsOf);
    }

    [Fact]
    public async Task A_warehouse_that_cannot_supply_alone_has_no_date_but_together_they_can()
    {
        var gloves = NewProduct();
        var check = Check([gloves], Level(gloves, _jhb, 6m), Level(gloves, _dbn, 6m));

        var stock = await Checked(check, "ACM-PPE-101", 10m);

        Assert.Equal(StockOutlook.Now, stock.Outlook);
        Assert.All(stock.Warehouses, w => Assert.Null(w.AvailableDate));
    }

    [Fact]
    public async Task A_shortfall_covered_by_a_receipt_ships_later_on_the_receipt_date()
    {
        var gloves = NewProduct();
        var receipt = _today.AddDays(9);
        var check = Check([gloves], Level(gloves, _jhb, 2m, allocated: 2m, onOrder: 50m, receipt: receipt));

        var stock = await Checked(check, "ACM-PPE-101", 10m);

        Assert.Equal(StockOutlook.Later, stock.Outlook);
        Assert.Equal(receipt, stock.AvailableDate);
        Assert.Equal(receipt, stock.Warehouses.Single().NextReceiptDate);
        Assert.Equal(50m, stock.Warehouses.Single().OnOrder);
    }

    [Fact]
    public async Task A_shortfall_with_nothing_on_order_is_unknown_not_guessed()
    {
        var gloves = NewProduct();
        var check = Check([gloves], Level(gloves, _jhb, 1m, allocated: 1m));

        var stock = await Checked(check, "ACM-PPE-101");

        Assert.Equal(StockOutlook.Unknown, stock.Outlook);
        Assert.Null(stock.AvailableDate);
        Assert.Equal(0m, stock.TotalAvailable);
    }

    [Fact]
    public async Task The_quantity_defaults_to_one()
    {
        var gloves = NewProduct();

        Assert.Equal(1m, (await Checked(Check([gloves], Level(gloves, _jhb, 1m)), "ACM-PPE-101")).Quantity);
    }

    [Fact]
    public async Task The_offering_can_be_its_id_or_a_lower_case_sku()
    {
        var gloves = NewProduct();
        var check = Check([gloves], Level(gloves, _jhb, 3m));

        Assert.Equal(gloves.Id, (await Checked(check, gloves.Id.ToString())).OfferingId);
        Assert.Equal(gloves.Id, (await Checked(check, " acm-ppe-101 ")).OfferingId);
    }

    [Fact]
    public async Task Warehouse_names_are_cleaned_as_untrusted_text()
    {
        var gloves = NewProduct();

        var stock = await Checked(Check([gloves], Level(gloves, _dbn, 3m)), "ACM-PPE-101");

        Assert.Equal("Durban centre", stock.Warehouses.Single().Name);
    }

    [Theory]
    [InlineData(null, "OfferingRequired")]
    [InlineData("  ", "OfferingRequired")]
    [InlineData("ACM-NOPE-999", "OfferingNotFound")]
    [InlineData("not a sku!", "OfferingNotFound")]
    [InlineData("0197a000-0000-7000-8000-000000000000", "OfferingNotFound")]
    public async Task Unusable_offerings_are_refused_with_a_code(string? offering, string code) =>
        Assert.Equal(code, (await Refused(Check([NewProduct()]), offering)).Code);

    [Fact]
    public async Task Another_business_s_product_is_not_found_even_by_id()
    {
        var theirs = NewProduct(business: BusinessId.New());

        Assert.Equal(OfferingLookupErrors.OfferingNotFound, await Refused(Check([theirs]), theirs.Id.ToString()));
    }

    [Fact]
    public async Task A_discontinued_product_is_refused()
    {
        var old = NewProduct();
        old.Deactivate();

        Assert.Equal(OfferingLookupErrors.OfferingInactive, await Refused(Check([old]), "ACM-PPE-101"));
    }

    [Fact]
    public async Task A_service_has_no_stock()
    {
        var service = Service.Create(OfferingId.New(), _acme, "ACM-SVC-001", "Bearing install", "", VatCode.Standard, ServicePricingModel.Fixed).Value;

        Assert.Equal(CheckStockErrors.OfferingNotStocked, await Refused(Check([service]), "ACM-SVC-001"));
    }

    [Theory]
    [InlineData(0, "QuantityNotPositive")]
    [InlineData(-3, "QuantityNotPositive")]
    [InlineData(1_000_001, "QuantityTooLarge")]
    public async Task Out_of_range_quantities_are_refused(int quantity, string code) =>
        Assert.Equal(code, (await Refused(Check([NewProduct()]), "ACM-PPE-101", quantity)).Code);

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeCatalog(Offering[] offerings) : ICatalogReader
    {
        public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
            Task.FromResult(offerings.FirstOrDefault(o => o.Id == id));

        public Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Offering>>([.. offerings.Where(o => o.BusinessId == businessId && o.IsActive)]);

        public Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken) =>
            Task.FromResult(offerings.FirstOrDefault(o => o.BusinessId == businessId && o.Sku == sku));

        public Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult(new OfferingRuleSet());
    }

    private sealed class FakeStock(StockLevel[] levels, Warehouse[] warehouses) : IStockReader
    {
        public Task<IReadOnlyList<StockLevel>> GetStockAsync(OfferingId offeringId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StockLevel>>([.. levels.Where(l => l.OfferingId == offeringId)]);

        public Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Warehouse>>([.. warehouses.Where(w => w.BusinessId == businessId).OrderBy(w => w.Code, StringComparer.Ordinal)]);
    }
}
