using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Pricing;

/// <summary>
/// A price agreed with one customer for one offering over a date range. It beats every price list
/// while it is valid, whatever the quantity.
/// </summary>
public sealed class ContractPrice
{
#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private ContractPrice()
    {
    }
#pragma warning restore CS8618

    private ContractPrice(CustomerId customerId, OfferingId offeringId, Money unitPrice, DateOnly validFrom, DateOnly validTo)
    {
        CustomerId = customerId;
        OfferingId = offeringId;
        UnitPrice = unitPrice;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    /// <summary>The customer the price was agreed with.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>The offering it covers.</summary>
    public OfferingId OfferingId { get; }

    /// <summary>Price per unit, excluding VAT.</summary>
    public Money UnitPrice { get; }

    /// <summary>First day it applies.</summary>
    public DateOnly ValidFrom { get; }

    /// <summary>Last day it applies (inclusive).</summary>
    public DateOnly ValidTo { get; }

    /// <summary>Validates and creates a contract price.</summary>
    public static Result<ContractPrice> Create(
        CustomerId customerId,
        OfferingId offeringId,
        Money unitPrice,
        DateOnly validFrom,
        DateOnly validTo)
    {
        if (!Money.IsValidCurrency(unitPrice.Currency))
        {
            return Result.Failure<ContractPrice>(PricingErrors.CurrencyInvalid);
        }

        if (unitPrice.Amount <= 0m)
        {
            return Result.Failure<ContractPrice>(PricingErrors.PriceNotPositive);
        }

        return validTo < validFrom
            ? Result.Failure<ContractPrice>(PricingErrors.ContractDatesInvalid)
            : Result.Success(new ContractPrice(customerId, offeringId, unitPrice, validFrom, validTo));
    }

    /// <summary>True when the contract applies on <paramref name="date"/>.</summary>
    public bool IsValidOn(DateOnly date) => date >= ValidFrom && date <= ValidTo;
}
