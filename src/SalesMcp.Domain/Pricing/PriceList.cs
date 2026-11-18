using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Pricing;

/// <summary>
/// One price for an offering from a minimum quantity upwards. Several items for the same offering
/// form quantity breaks, for example 1+, 10+ and 50+.
/// </summary>
/// <param name="OfferingId">The offering priced.</param>
/// <param name="MinQuantity">The smallest quantity this price applies to.</param>
/// <param name="UnitPrice">Price per unit, excluding VAT. Always greater than zero.</param>
public sealed record PriceListItem(OfferingId OfferingId, Quantity MinQuantity, Money UnitPrice);

/// <summary>
/// A set of prices in one currency. Each business has a default list (its list prices); a customer
/// may also have a list of their own, which is tried first.
/// </summary>
public sealed class PriceList
{
    private readonly List<PriceListItem> _items = [];

    private PriceList(PriceListId id, BusinessId businessId, string name, string currency)
    {
        Id = id;
        BusinessId = businessId;
        Name = name;
        Currency = currency;
    }

    /// <summary>The list's id.</summary>
    public PriceListId Id { get; }

    /// <summary>The business whose prices these are.</summary>
    public BusinessId BusinessId { get; }

    /// <summary>Display name, for example "Trade 2026".</summary>
    public string Name { get; }

    /// <summary>The currency of every price on the list.</summary>
    public string Currency { get; }

    /// <summary>The prices, in the order they were added.</summary>
    public IReadOnlyList<PriceListItem> Items => _items;

    /// <summary>Validates and creates an empty price list.</summary>
    public static Result<PriceList> Create(PriceListId id, BusinessId businessId, string? name, string? currency)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<PriceList>(PricingErrors.NameRequired);
        }

        if (trimmedName.Length > PricingErrors.MaxNameLength)
        {
            return Result.Failure<PriceList>(PricingErrors.NameTooLong);
        }

        return Money.IsValidCurrency(currency)
            ? Result.Success(new PriceList(id, businessId, trimmedName, currency!))
            : Result.Failure<PriceList>(PricingErrors.CurrencyInvalid);
    }

    /// <summary>
    /// Adds a price from <paramref name="minQuantity"/> upwards. The price must be positive and in the
    /// list's currency, and the list can't already have a price for that offering at that quantity.
    /// </summary>
    public Result AddItem(OfferingId offeringId, Quantity minQuantity, Money unitPrice)
    {
        if (unitPrice.Currency != Currency)
        {
            return Result.Failure(PricingErrors.CurrencyMismatch);
        }

        if (unitPrice.Amount <= 0m)
        {
            return Result.Failure(PricingErrors.PriceNotPositive);
        }

        if (_items.Any(i => i.OfferingId == offeringId && i.MinQuantity == minQuantity))
        {
            return Result.Failure(PricingErrors.BreakDuplicate);
        }

        _items.Add(new PriceListItem(offeringId, minQuantity, unitPrice));
        return Result.Success();
    }

    /// <summary>
    /// The price that applies to <paramref name="quantity"/>: the item with the largest minimum quantity
    /// not above it. Null when the list has no price for the offering, or only breaks above the quantity.
    /// </summary>
    public PriceListItem? BestBreak(OfferingId offeringId, Quantity quantity) =>
        _items
            .Where(i => i.OfferingId == offeringId && i.MinQuantity <= quantity)
            .MaxBy(i => i.MinQuantity);
}
