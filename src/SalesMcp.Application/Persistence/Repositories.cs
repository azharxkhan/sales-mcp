using SalesMcp.Domain.Common;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Application.Persistence;

/// <summary>Stores and loads quotes. Changes are saved by <see cref="IUnitOfWork"/>.</summary>
public interface IQuoteRepository
{
    /// <summary>The quote with this id, with its lines, or null.</summary>
    Task<Quote?> GetAsync(QuoteId id, CancellationToken cancellationToken);

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

/// <summary>Saves everything changed through the repositories in one transaction.</summary>
public interface IUnitOfWork
{
    /// <summary>Saves pending changes.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
