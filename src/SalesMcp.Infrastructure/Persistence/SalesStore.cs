using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Infrastructure.Persistence;

/// <summary>Read-only queries. Entities come back untracked, so reads can't be saved by accident.</summary>
internal sealed class SalesReader(SalesDbContext db) : IBusinessReader, IOrderReader, ICatalogReader, ICustomerReader, IPricingReader, IStockReader
{
    public Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken cancellationToken) =>
        db.Businesses.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    // The customer is part of the query, so another customer's order is simply not found (D2).
    public Task<SalesOrder?> FindByNumberAsync(CustomerId customerId, OrderNumber number, CancellationToken cancellationToken) =>
        db.SalesOrders.AsNoTracking().FirstOrDefaultAsync(o => o.Number == number && o.CustomerId == customerId, cancellationToken);

    public async Task<QuoteNumber?> GetQuoteNumberAsync(QuoteId id, CancellationToken cancellationToken)
    {
        var numbers = await db.Quotes.AsNoTracking()
            .Where(q => q.Id == id)
            .Select(q => q.Number)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return numbers.Count == 0 ? null : numbers[0];
    }

    public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
        db.Offerings.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
        await db.Offerings.AsNoTracking()
            .Where(o => o.BusinessId == businessId && o.IsActive)
            .OrderBy(o => o.Sku)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    public Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken) =>
        db.Offerings.AsNoTracking().FirstOrDefaultAsync(o => o.BusinessId == businessId && o.Sku == sku, cancellationToken);

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

    public async Task<IReadOnlyList<Warehouse>> ListWarehousesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
        await db.Warehouses.AsNoTracking()
            .Where(w => w.BusinessId == businessId)
            .OrderBy(w => w.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
}

/// <summary>Tracked access to the documents agents create, saved together through <see cref="IUnitOfWork"/>.</summary>
internal sealed class SalesDocuments(SalesDbContext db) : IQuoteRepository, IOrderRepository, IQuoteNumbers, IIdempotencyStore, IUnitOfWork
{
    public Task<Quote?> GetAsync(QuoteId id, CancellationToken cancellationToken) =>
        db.Quotes.FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

    public Task<Quote?> FindByNumberAsync(CustomerId customerId, QuoteNumber number, CancellationToken cancellationToken) =>
        db.Quotes.FirstOrDefaultAsync(q => q.Number == number && q.CustomerId == customerId, cancellationToken);

    public void Add(Quote quote) => db.Quotes.Add(quote);

    public Task<SalesOrder?> GetAsync(OrderId id, CancellationToken cancellationToken) =>
        db.SalesOrders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public void Add(SalesOrder order) => db.SalesOrders.Add(order);

    public async Task<Result<QuoteNumber>> NextAsync(int year, CancellationToken cancellationToken)
    {
        // Numbers are fixed-width (Q-2026-000123), so the text order is the number order.
        var prefix = string.Create(CultureInfo.InvariantCulture, $"Q-{year:D4}-%");
        var highest = await db.Database
            .SqlQuery<string>($"SELECT \"Number\" AS \"Value\" FROM \"Quotes\" WHERE \"Number\" LIKE {prefix} ORDER BY \"Number\" DESC LIMIT 1")
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var last = highest.Count == 0 ? 0 : int.Parse(highest[0].AsSpan(7), CultureInfo.InvariantCulture);
        return QuoteNumber.From(year, last + 1);
    }

    public Task<IdempotencyRecord?> FindAsync(string key, string tool, CustomerId customerId, CancellationToken cancellationToken) =>
        db.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key && r.Tool == tool && r.CustomerId == customerId, cancellationToken);

    public void Add(IdempotencyRecord record) => db.IdempotencyRecords.Add(record);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    // SQLite: Microsoft.Data.Sqlite starts a non-deferred transaction with BEGIN IMMEDIATE, which takes
    // the write lock at once, so concurrent calls queue instead of reading the same next number.
    public async Task<IWriteTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new WriteTransaction(await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));

    private sealed class WriteTransaction(IDbContextTransaction transaction) : IWriteTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
