using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Domain.Quotes;

/// <summary>An offering and the price breakdown it was quoted with, as input to <see cref="Quote.Create"/>.</summary>
/// <param name="Offering">The offering quoted.</param>
/// <param name="Price">Its breakdown from the price resolver.</param>
public sealed record PricedOffering(Offering Offering, PriceBreakdown Price);

/// <summary>
/// One line of a quote. A quote is a commitment (ADR-0015), so the line keeps a snapshot of the item
/// and its price as quoted. Later changes to the catalog or price lists don't change it.
/// </summary>
public sealed class QuoteLine
{
#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private QuoteLine()
    {
    }
#pragma warning restore CS8618

    internal QuoteLine(int lineNumber, Offering offering, PriceBreakdown price)
    {
        LineNumber = lineNumber;
        OfferingId = offering.Id;
        Sku = offering.Sku;
        Name = offering.Name;
        Unit = (offering as Product)?.Unit;

        // Copy the steps so the snapshot can't be changed through the original list.
        Price = price with { Steps = [.. price.Steps] };
    }

    /// <summary>Position on the quote, from 1.</summary>
    public int LineNumber { get; }

    /// <summary>The offering quoted.</summary>
    public OfferingId OfferingId { get; }

    /// <summary>The SKU when quoted.</summary>
    public Sku Sku { get; }

    /// <summary>The name when quoted.</summary>
    public string Name { get; }

    /// <summary>The unit the quantity is in, when quoted; products only.</summary>
    public UnitOfMeasure? Unit { get; }

    /// <summary>The price breakdown when quoted.</summary>
    public PriceBreakdown Price { get; }

    /// <summary>The quantity quoted.</summary>
    public Quantity Quantity => Price.Quantity;

    /// <summary>The line after discount, excluding VAT.</summary>
    public Money Net => Price.Net;

    /// <summary>The line's VAT.</summary>
    public Money Vat => Price.Vat.Vat;

    /// <summary>The line including VAT.</summary>
    public Money Total => Price.Total;
}
