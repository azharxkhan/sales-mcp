using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Pricing;

/// <summary>What a step in the price calculation did.</summary>
public enum PriceStepKind
{
    /// <summary>The unit price came from a valid contract.</summary>
    ContractPrice = 0,

    /// <summary>The unit price came from the customer's own price list.</summary>
    CustomerPriceList = 1,

    /// <summary>The unit price came from the business's default price list.</summary>
    DefaultPriceList = 2,

    /// <summary>A discount was taken off the line.</summary>
    Discount = 3,

    /// <summary>VAT was added to the line.</summary>
    Vat = 4,
}

/// <summary>One step in the price calculation, so the agent can explain the price.</summary>
/// <param name="Kind">What the step did.</param>
/// <param name="Detail">A plain description, for example "Trade 2026, 10+ break".</param>
/// <param name="Amount">The amount the step produced: the unit price, the discount (negative) or the VAT.</param>
public sealed record PriceStep(PriceStepKind Kind, string Detail, Money Amount);

/// <summary>
/// The result of pricing one line: every figure, the steps that produced them and when it was worked
/// out. The only way a price reaches a quote or a buyer (guardrail A1).
/// </summary>
/// <param name="OfferingId">The offering priced.</param>
/// <param name="CustomerId">The customer it was priced for.</param>
/// <param name="Quantity">The quantity priced.</param>
/// <param name="Source">Where the unit price came from: a contract, the customer's list or the default list.</param>
/// <param name="UnitPrice">Price per unit before discount, excluding VAT.</param>
/// <param name="Gross">Unit price times quantity, rounded.</param>
/// <param name="DiscountRate">The discount applied (0% when none).</param>
/// <param name="Discount">The discount amount, rounded. Zero or positive.</param>
/// <param name="Vat">Net, VAT and total for the line, rounded per line (ADR-0007).</param>
/// <param name="AsOf">When the price was worked out. Prices are facts "as of" this time (guardrail B1).</param>
/// <param name="Steps">The steps, in order.</param>
public sealed record PriceBreakdown(
    OfferingId OfferingId,
    CustomerId CustomerId,
    Quantity Quantity,
    PriceStepKind Source,
    Money UnitPrice,
    Money Gross,
    Percentage DiscountRate,
    Money Discount,
    VatLine Vat,
    DateTimeOffset AsOf,
    IReadOnlyList<PriceStep> Steps)
{
    /// <summary>The line after discount, excluding VAT.</summary>
    public Money Net => Vat.Net;

    /// <summary>The line including VAT.</summary>
    public Money Total => Vat.Total;
}
