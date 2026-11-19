using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Orders;

/// <summary>Where an order is in fulfilment. Transitions arrive in M4; M1 orders start as drafts.</summary>
public enum OrderStatus
{
    /// <summary>Created from a quote, not yet released (credit and stock checks come at release).</summary>
    Draft = 0,

    /// <summary>Passed credit checks; stock is allocated.</summary>
    Released = 1,

    /// <summary>Some lines have shipped.</summary>
    PartiallyShipped = 2,

    /// <summary>Everything has shipped.</summary>
    Shipped = 3,

    /// <summary>Invoiced.</summary>
    Invoiced = 4,

    /// <summary>Cancelled before shipping.</summary>
    Cancelled = 5,
}

/// <summary>One order line, copied from the accepted quote line so the agreed price carries over.</summary>
/// <param name="LineNumber">Position on the order, from 1.</param>
/// <param name="OfferingId">The offering ordered.</param>
/// <param name="Sku">The SKU as quoted.</param>
/// <param name="Name">The name as quoted.</param>
/// <param name="Quantity">Quantity ordered.</param>
/// <param name="UnitPrice">Unit price before discount, as quoted.</param>
/// <param name="Vat">Net, VAT and total for the line, as quoted.</param>
public sealed record OrderLine(
    int LineNumber,
    OfferingId OfferingId,
    Sku Sku,
    string Name,
    Quantity Quantity,
    Money UnitPrice,
    VatLine Vat)
{
    /// <summary>The line after discount, excluding VAT.</summary>
    public Money Net => Vat.Net;

    /// <summary>The line including VAT.</summary>
    public Money Total => Vat.Total;
}

/// <summary>
/// A customer's order. Orders exist only by converting a quote the buyer confirmed (ADR-0015), so each
/// order records its source quote and charges exactly what was quoted.
/// </summary>
/// <remarks>
/// M1 is a skeleton. Buyer confirmation, credit holds, allocation, back-orders and the status
/// machine arrive in M4.
/// </remarks>
public sealed class SalesOrder
{
    private readonly List<OrderLine> _lines;

    private SalesOrder(OrderId id, Quote quote, DateOnly orderedOn, List<OrderLine> lines)
    {
        Id = id;
        CustomerId = quote.CustomerId;
        AgentId = quote.AgentId;
        SourceQuoteId = quote.Id;
        OrderedOn = orderedOn;
        Currency = quote.Currency;
        _lines = lines;
        Status = OrderStatus.Draft;
    }

    /// <summary>The order's id.</summary>
    public OrderId Id { get; }

    /// <summary>The customer.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>The agent that issued the source quote.</summary>
    public AgentId AgentId { get; }

    /// <summary>The quote this order was converted from.</summary>
    public QuoteId SourceQuoteId { get; }

    /// <summary>The day the quote was converted.</summary>
    public DateOnly OrderedOn { get; }

    /// <summary>The order's currency.</summary>
    public string Currency { get; }

    /// <summary>Where it is in fulfilment.</summary>
    public OrderStatus Status { get; }

    /// <summary>The lines, numbered as on the quote.</summary>
    public IReadOnlyList<OrderLine> Lines => _lines;

    /// <summary>Net, VAT and total, with VAT shown per code.</summary>
    public VatSummary Totals => VatSummary.Of(_lines.Select(l => l.Vat), Currency);

    /// <summary>
    /// Converts an open, unexpired quote into a draft order at the quoted prices and marks the quote
    /// accepted, so it can't be converted twice.
    /// </summary>
    /// <param name="id">The order's id.</param>
    /// <param name="quote">The quote the buyer confirmed.</param>
    /// <param name="today">Today, from the caller's clock.</param>
    public static Result<SalesOrder> FromQuote(OrderId id, Quote quote, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var acceptable = quote.CanBeAccepted(today);
        if (acceptable.IsFailure)
        {
            return Result.Failure<SalesOrder>(acceptable.Error);
        }

        var lines = quote.Lines
            .Select(l => new OrderLine(l.LineNumber, l.OfferingId, l.Sku, l.Name, l.Quantity, l.Price.UnitPrice, l.Price.Vat))
            .ToList();

        quote.MarkAccepted();
        return Result.Success(new SalesOrder(id, quote, today, lines));
    }
}
