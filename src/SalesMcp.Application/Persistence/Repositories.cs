using SalesMcp.Domain.Common;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Application.Persistence;

/// <summary>Stores and loads quotes. Changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IQuoteRepository
{
    /// <summary>The quote with this id, with its lines, or null.</summary>
    Task<Quote?> GetAsync(QuoteId id, CancellationToken cancellationToken);

    /// <summary>The customer's quote with this number, with its lines, or null.</summary>
    Task<Quote?> FindByNumberAsync(CustomerId customerId, QuoteNumber number, CancellationToken cancellationToken);

    /// <summary>Adds a new quote.</summary>
    void Add(Quote quote);
}

/// <summary>Stores and loads sales orders. Changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IOrderRepository
{
    /// <summary>The order with this id, with its lines, or null.</summary>
    Task<SalesOrder?> GetAsync(OrderId id, CancellationToken cancellationToken);

    /// <summary>Adds a new order.</summary>
    void Add(SalesOrder order);
}

/// <summary>Issues quote numbers. Changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IQuoteNumbers
{
    /// <summary>
    /// The next number in <paramref name="year"/>: one more than the highest stored, so numbers have
    /// no gaps or repeats. Call it inside <see cref="IUnitOfWork.BeginTransactionAsync"/>, which holds
    /// the write lock until the quote is saved.
    /// </summary>
    Task<Result<QuoteNumber>> NextAsync(int year, CancellationToken cancellationToken);
}

/// <summary>Stores the first result of each mutating call (guardrail A8). Changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IIdempotencyStore
{
    /// <summary>The record for this key, tool and customer, expired or not, or null.</summary>
    Task<IdempotencyRecord?> FindAsync(string key, string tool, CustomerId customerId, CancellationToken cancellationToken);

    /// <summary>Adds a new record.</summary>
    void Add(IdempotencyRecord record);
}

/// <summary>Saves everything changed through the repositories in one transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Saves pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Starts a transaction that takes the write lock at once, so two calls that read-then-write
    /// (the next quote number, an idempotency key) run one after the other. Dispose without
    /// committing to roll back.
    /// </summary>
    Task<IWriteTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

/// <summary>A transaction from <see cref="IUnitOfWork.BeginTransactionAsync"/>.</summary>
public interface IWriteTransaction : IAsyncDisposable
{
    /// <summary>Commits everything saved since it began.</summary>
    Task CommitAsync(CancellationToken cancellationToken);
}
