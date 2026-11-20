using SalesMcp.Domain.Common;

namespace SalesMcp.Infrastructure.Seeding;

/// <summary>
/// Named rows in a seeded dataset, so tests and evaluation scenarios can say "the customer on hold"
/// instead of hunting for one. Every id is stable across runs and machines.
/// </summary>
/// <param name="Business">The seeded business.</param>
/// <param name="Agent">The AI agent that issued the recent quotes.</param>
/// <param name="Warehouses">Named warehouses.</param>
/// <param name="PriceLists">Named price lists.</param>
/// <param name="Customers">Customers with a particular trait.</param>
/// <param name="Offerings">Offerings with a particular trait.</param>
public sealed record SeedManifest(
    BusinessId Business,
    AgentId Agent,
    SeedWarehouses Warehouses,
    SeedPriceLists PriceLists,
    SeedCustomers Customers,
    SeedOfferings Offerings);

/// <summary>The seeded warehouses.</summary>
/// <param name="Johannesburg">The main distribution centre.</param>
/// <param name="Durban">The coastal distribution centre, holding less stock.</param>
public sealed record SeedWarehouses(WarehouseId Johannesburg, WarehouseId Durban);

/// <summary>The seeded price lists.</summary>
/// <param name="Default">The default list, with quantity breaks on cheaper consumables.</param>
/// <param name="Mining">A customer list for mining accounts.</param>
/// <param name="Trade">A customer list for trade partners.</param>
public sealed record SeedPriceLists(PriceListId Default, PriceListId Mining, PriceListId Trade);

/// <summary>Customers with a particular trait.</summary>
/// <param name="Top">The largest customer by revenue, with contract prices and the mining list.</param>
/// <param name="NearCreditLimit">Balance above 90% of the credit limit, not on hold.</param>
/// <param name="OnHold">On credit hold, with a balance over the limit.</param>
/// <param name="NoBackOrders">Doesn't accept back-orders or partial shipments.</param>
/// <param name="New">Has quotes but has never placed an order.</param>
/// <param name="CashOnly">Pays cash with order and has no VAT number.</param>
public sealed record SeedCustomers(
    CustomerId Top,
    CustomerId NearCreditLimit,
    CustomerId OnHold,
    CustomerId NoBackOrders,
    CustomerId New,
    CustomerId CashOnly);

/// <summary>Offerings with a particular trait.</summary>
/// <param name="InStock">Available in both warehouses.</param>
/// <param name="ShortWithReceipt">Nothing available, but stock is on order with a receipt date.</param>
/// <param name="ShortWithoutReceipt">Nothing available and nothing on order.</param>
/// <param name="Discontinued">Inactive, so it can't be quoted.</param>
/// <param name="QuantityBreaks">Cheaper per unit at 10 and at 50 on the default list.</param>
/// <param name="ZeroRated">Zero-rated for VAT.</param>
/// <param name="Exempt">Exempt from VAT.</param>
/// <param name="DrillBody">A drill sold without a battery; requires <paramref name="Battery"/>.</param>
/// <param name="Battery">The battery the drill body requires.</param>
public sealed record SeedOfferings(
    OfferingId InStock,
    OfferingId ShortWithReceipt,
    OfferingId ShortWithoutReceipt,
    OfferingId Discontinued,
    OfferingId QuantityBreaks,
    OfferingId ZeroRated,
    OfferingId Exempt,
    OfferingId DrillBody,
    OfferingId Battery);
