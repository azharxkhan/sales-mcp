using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Application.Persistence;

/// <summary>Reads businesses.</summary>
public interface IBusinessReader
{
    /// <summary>The business with this id, or null.</summary>
    Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken cancellationToken);
}

/// <summary>Reads the catalog. Narrow on purpose: tools ask for exactly what they need.</summary>
public interface ICatalogReader
{
    /// <summary>The offering with this id, or null.</summary>
    Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken);

    /// <summary>The business's active offerings, ordered by SKU.</summary>
    Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken);

    /// <summary>The business's offering with this SKU, active or not, or null.</summary>
    Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken);

    /// <summary>Every configuration rule that starts at one of the business's offerings.</summary>
    Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken);
}

/// <summary>Reads customers and their contacts.</summary>
public interface ICustomerReader
{
    /// <summary>The customer with this id, or null.</summary>
    Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken);

    /// <summary>The customer's contacts, ordered by name.</summary>
    Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>
    /// Customers with this account number, in any business, at most two: enough to tell a unique match
    /// from an ambiguous one. Account numbers are unique within a business, so two matches means two
    /// businesses use the same number.
    /// </summary>
    Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken);
}

/// <summary>Loads what the price resolver needs for one customer.</summary>
public interface IPricingReader
{
    /// <summary>
    /// The business's default list, the customer's own list (if any) and the customer's contract prices.
    /// Null when the business has no default list.
    /// </summary>
    Task<PriceSources?> GetPriceSourcesAsync(Customer customer, CancellationToken cancellationToken);
}

/// <summary>Reads stock levels.</summary>
public interface IStockReader
{
    /// <summary>Stock for the offering in every warehouse that holds a record for it.</summary>
    Task<IReadOnlyList<StockLevel>> GetStockAsync(OfferingId offeringId, CancellationToken cancellationToken);

    /// <summary>The business's warehouses, ordered by code.</summary>
    Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(BusinessId businessId, CancellationToken cancellationToken);
}
