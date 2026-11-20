using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>
/// The demo dataset for <b>Acme Distributors</b>, an invented South African industrial-supplies
/// distributor: 120 products in 8 categories, 2 warehouses, 40 customers, price lists and contracts,
/// and about 12 months of quotes and orders shaped by the benchmarks in
/// <c>docs/research/sales-data-benchmarks.md</c>.
/// </summary>
/// <remarks>
/// Built from a fixed random seed, so the same <c>today</c> always gives the same rows. Dates are
/// relative to <c>today</c> so the demo never goes stale: open quotes stay open and receipts stay in
/// the future. Every row is created through the domain factories, so seed data obeys the same rules
/// as data entered through the tools.
/// </remarks>
public static class AcmeSeed
{
    /// <summary>The random seed. Changing it changes every generated value except the fixtures.</summary>
    public const int RandomSeed = 20_261_119;

    /// <summary>How far back the quote history goes.</summary>
    public const int HistoryDays = 365;

    /// <summary>Quotes issued this recently are still open: not yet won or lost.</summary>
    public const int OpenQuoteDays = 14;

    /// <summary>Target quote win rate for customers with no orders yet.</summary>
    public const double NewCustomerWinRate = 0.20;

    /// <summary>Target quote win rate for customers who have ordered before.</summary>
    public const double ExistingCustomerWinRate = 0.45;

    /// <summary>The seed's currency.</summary>
    public const string Currency = "ZAR";

    /// <summary>Named rows for tests and evaluation scenarios.</summary>
    public static SeedManifest Manifest { get; } = new(
        new BusinessId(SeedIds.For("acme/business")),
        AgentId.Create("acme-distributor-jolt").Value,
        new SeedWarehouses(WarehouseIdFor("JHB"), WarehouseIdFor("DBN")),
        new SeedPriceLists(PriceListIdFor("default"), PriceListIdFor("mining"), PriceListIdFor("trade")),
        new SeedCustomers(
            CustomerIdFor(AcmeCatalog.TopRank),
            CustomerIdFor(AcmeCatalog.NearCreditLimitRank),
            CustomerIdFor(AcmeCatalog.OnHoldRank),
            CustomerIdFor(AcmeCatalog.NoBackOrdersRank),
            CustomerIdFor(AcmeCatalog.NewRank),
            CustomerIdFor(AcmeCatalog.CashOnlyRank)),
        new SeedOfferings(
            OfferingIdFor(AcmeCatalog.InStockSku),
            OfferingIdFor(AcmeCatalog.ShortWithReceiptSku),
            OfferingIdFor(AcmeCatalog.ShortWithoutReceiptSku),
            OfferingIdFor(AcmeCatalog.DiscontinuedSku),
            OfferingIdFor(AcmeCatalog.QuantityBreaksSku),
            OfferingIdFor(AcmeCatalog.ZeroRatedSku),
            OfferingIdFor(AcmeCatalog.ExemptSku),
            OfferingIdFor(AcmeCatalog.DrillBodySku),
            OfferingIdFor(AcmeCatalog.BatterySku)));

    /// <summary>Builds the dataset as of <paramref name="today"/>, without touching a database.</summary>
    public static SeedDataset Build(DateOnly today) => new AcmeBuilder(today).Build();

    internal static OfferingId OfferingIdFor(string sku) => new(SeedIds.For("acme/offering/" + sku));

    internal static WarehouseId WarehouseIdFor(string code) => new(SeedIds.For("acme/warehouse/" + code));

    internal static PriceListId PriceListIdFor(string name) => new(SeedIds.For("acme/price-list/" + name));

    internal static CustomerId CustomerIdFor(int rank) => new(SeedIds.For("acme/customer/" + AcmeCatalog.AccountNumberFor(rank)));
}

/// <summary>Every row of a seeded business, ready to add to a context.</summary>
/// <param name="Manifest">Named rows.</param>
/// <param name="Business">The business.</param>
/// <param name="Offerings">Its catalog, including discontinued offerings.</param>
/// <param name="Rules">Configuration rules between offerings.</param>
/// <param name="Warehouses">Its warehouses.</param>
/// <param name="StockLevels">Stock per offering and warehouse.</param>
/// <param name="PriceLists">The default list and the customer lists.</param>
/// <param name="ContractPrices">Customer contract prices.</param>
/// <param name="Customers">Its customers.</param>
/// <param name="Contacts">People at those customers.</param>
/// <param name="Quotes">Quote history, oldest first.</param>
/// <param name="Orders">Orders converted from the won quotes.</param>
public sealed record SeedDataset(
    SeedManifest Manifest,
    Business Business,
    IReadOnlyList<Offering> Offerings,
    IReadOnlyList<OfferingRule> Rules,
    IReadOnlyList<Warehouse> Warehouses,
    IReadOnlyList<StockLevel> StockLevels,
    IReadOnlyList<PriceList> PriceLists,
    IReadOnlyList<ContractPrice> ContractPrices,
    IReadOnlyList<Customer> Customers,
    IReadOnlyList<Contact> Contacts,
    IReadOnlyList<Quote> Quotes,
    IReadOnlyList<SalesOrder> Orders)
{
    /// <summary>All rows, for <c>DbContext.AddRange</c>.</summary>
    public IEnumerable<object> AllRows() =>
        new object[] { Business }
            .Concat(Offerings)
            .Concat(Rules)
            .Concat(Warehouses)
            .Concat(StockLevels)
            .Concat(PriceLists)
            .Concat(ContractPrices)
            .Concat(Customers)
            .Concat(Contacts)
            .Concat(Quotes)
            .Concat(Orders);
}
