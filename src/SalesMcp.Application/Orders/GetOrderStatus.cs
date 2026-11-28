using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Application.Orders;

/// <summary>Errors from looking up an order.</summary>
public static class OrderQueryErrors
{
    /// <summary>No order number was given.</summary>
    public static readonly DomainError OrderNumberRequired = new(
        "OrderNumberRequired", "Give the order number, for example SO-2026-000123.");

    /// <summary>The customer has no order with that number (or it is another customer's).</summary>
    public static readonly DomainError OrderNotFound = new(
        "OrderNotFound", "No order with that number was found on this account. Check the number on the order confirmation.");
}

/// <summary>An order line ready to show.</summary>
/// <param name="Line">The line as ordered.</param>
/// <param name="Name">The name as ordered, cleaned as untrusted text.</param>
/// <param name="Unit">The unit the quantity is in; products only.</param>
public sealed record OrderStatusLine(OrderLine Line, string Name, UnitOfMeasure? Unit);

/// <summary>Where an order stands.</summary>
/// <param name="Order">The order.</param>
/// <param name="QuoteNumber">The quote it was converted from, if it still exists.</param>
/// <param name="StatusDescription">The status in words for the buyer.</param>
/// <param name="Lines">The lines, in order.</param>
/// <param name="AsOf">When the status was read (guardrail B1).</param>
public sealed record OrderStatusView(
    SalesOrder Order,
    QuoteNumber? QuoteNumber,
    string StatusDescription,
    IReadOnlyList<OrderStatusLine> Lines,
    DateTimeOffset AsOf);

/// <summary>
/// Looks up one of the session's customer's orders by its number. Another customer's number is "not
/// found", never "forbidden", so numbers can't be probed (guardrail D2).
/// </summary>
/// <param name="orders">Reads orders, scoped to the customer.</param>
/// <param name="catalog">Reads offerings for their units.</param>
/// <param name="time">The clock for <c>asOf</c>.</param>
public sealed class GetOrderStatus(IOrderReader orders, ICatalogReader catalog, TimeProvider time)
{
    /// <summary>The status of <paramref name="orderNumber"/> for <paramref name="customerId"/>.</summary>
    /// <param name="customerId">The session's customer. Never a model argument (guardrail D2).</param>
    /// <param name="orderNumber">The order number, unchecked.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task<Result<OrderStatusView>> ExecuteAsync(CustomerId customerId, string? orderNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            return Result.Failure<OrderStatusView>(OrderQueryErrors.OrderNumberRequired);
        }

        var number = OrderNumber.Parse(orderNumber);
        if (number.IsFailure)
        {
            return Result.Failure<OrderStatusView>(number.Error);
        }

        var order = await orders.FindByNumberAsync(customerId, number.Value, cancellationToken).ConfigureAwait(false);
        if (order is null || order.CustomerId != customerId)
        {
            return Result.Failure<OrderStatusView>(OrderQueryErrors.OrderNotFound);
        }

        var quoteNumber = await orders.GetQuoteNumberAsync(order.SourceQuoteId, cancellationToken).ConfigureAwait(false);
        var lines = new List<OrderStatusLine>(order.Lines.Count);
        foreach (var line in order.Lines.OrderBy(l => l.LineNumber))
        {
            var offering = await catalog.GetOfferingAsync(line.OfferingId, cancellationToken).ConfigureAwait(false);
            lines.Add(new OrderStatusLine(line, UntrustedText.Clean(line.Name, CatalogErrors.MaxNameLength), (offering as Product)?.Unit));
        }

        return Result.Success(new OrderStatusView(order, quoteNumber, Describe(order.Status), lines, time.GetUtcNow()));
    }

    /// <summary>A status in words a buyer understands.</summary>
    public static string Describe(OrderStatus status) => status switch
    {
        OrderStatus.Draft => "Received; waiting to be released for picking.",
        OrderStatus.Released => "Released; stock is set aside and the order is being picked.",
        OrderStatus.PartiallyShipped => "Part of the order has shipped; the rest follows.",
        OrderStatus.Shipped => "Shipped in full.",
        OrderStatus.Invoiced => "Shipped and invoiced.",
        OrderStatus.Cancelled => "Cancelled; nothing will ship.",
        _ => "Status unknown; a person will check it.",
    };
}
