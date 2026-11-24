using Microsoft.EntityFrameworkCore;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Infrastructure.Persistence;

/// <summary>Read-only queries. Entities come back untracked, so reads can't be saved by accident.</summary>
internal sealed class SalesReader(SalesDbContext db) : ICatalogReader, ICustomerReader, IPricingReader, IStockReader
{
    public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
        db.Offerings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
        await db.Offerings.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.IsActive)
            .OrderBy(o => o.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken)
    {
        var rules = await db.OfferingRules.AsNoTracking()
            .Where(r => db.Offerings.Any(o => o.Id == r.OfferingId && o.BusinessId == businessId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        // Stored rules were checked when added, so rebuilding the set can't fail; if it does, the data is corrupt.
        var set = new OfferingRuleSet();
        foreach (var rule in rules)
        {
            var added = set.Add(rule);
            if (added.IsFailure)
            {
                throw new InvalidOperationException($"Stored offering rules conflict: {added.Error.Code}.");
            }
        }

        return set;
    }

    public Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
        db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken) =>
        await db.Customers.AsNoTracking()
            .Where(c => c.AccountNumber == accountNumber)
            .OrderBy(c => c.BusinessId)
            .Take(2)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken) =>
        await db.Contacts.AsNoTracking()
            .Where(c => c.CustomerId == customerId)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public async Task<PriceSources?> GetPriceSourcesAsync(Customer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var lists = await db.PriceLists.AsNoTracking()
            .Where(p => p.BusinessId == customer.BusinessId && (p.IsDefault || p.Id == customer.PriceListId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var defaultList = lists.FirstOrDefault(p => p.IsDefault);
        if (defaultList is null)
        {
            return null;
        }

        var contracts = await db.ContractPrices.AsNoTracking()
            .Where(c => c.CustomerId == customer.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PriceSources(defaultList, lists.FirstOrDefault(p => p.Id == customer.PriceListId), contracts);
    }

    public async Task<IReadOnlyList<StockLevel>> GetStockAsync(OfferingId offeringId, CancellationToken cancellationToken) =>
        await db.StockLevels.AsNoTracking()
            .Where(s => s.OfferingId == offeringId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Tracked access to the documents agents create, saved together through <see cref="IUnitOfWork"/>.</summary>
internal sealed class SalesDocuments(SalesDbContext db) : IQuoteRepository, IOrderRepository, IUnitOfWork
{
    public Task<Quote?> GetAsync(QuoteId id, CancellationToken cancellationToken) =>
        db.Quotes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public void Add(Quote quote) => db.Quotes.Add(quote);

    public Task<SalesOrder?> GetAsync(OrderId id, CancellationToken cancellationToken) =>
        db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(SalesOrder order) => db.SalesOrders.Add(order);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
