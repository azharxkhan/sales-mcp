using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Pricing;

/// <summary>Business-rule failures for pricing. Codes are stable; tools and tests match on them.</summary>
public static class PricingErrors
{
    /// <summary>Longest allowed price list name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// No contract or price list has a price for the offering at this quantity. Never priced as zero
    /// (guardrail A1); the agent must say it can't quote and offer a human.
    /// </summary>
    public static DomainError NoPrice { get; } =
        new("NoPrice", "There is no price for this item at this quantity. Ask a person to quote it.");

    /// <summary>The offering is inactive, so it can't be priced.</summary>
    public static DomainError OfferingInactive { get; } =
        new("OfferingInactive", "This item is no longer sold.");

    /// <summary>A price is zero or negative.</summary>
    public static DomainError PriceNotPositive { get; } =
        new("PriceNotPositive", "A price must be greater than zero.");

    /// <summary>Amounts are in different currencies.</summary>
    public static DomainError CurrencyMismatch { get; } =
        new("CurrencyMismatch", "All prices must be in the price list's currency.");

    /// <summary>The currency isn't a three-letter ISO 4217 code.</summary>
    public static DomainError CurrencyInvalid { get; } =
        new("CurrencyInvalid", "The currency must be a three-letter ISO 4217 code such as ZAR.");

    /// <summary>The offering, customer or price list belong to different businesses.</summary>
    public static DomainError BusinessMismatch { get; } =
        new("BusinessMismatch", "The item, customer and price lists must belong to the same business.");

    /// <summary>The customer price list supplied isn't the one assigned to the customer.</summary>
    public static DomainError PriceListMismatch { get; } =
        new("PriceListMismatch", "The customer price list doesn't match the list assigned to the customer.");

    /// <summary>The list already has a price for this offering at this minimum quantity.</summary>
    public static DomainError BreakDuplicate { get; } =
        new("BreakDuplicate", "This price list already has a price for that item at that minimum quantity.");

    /// <summary>A contract ends before it starts.</summary>
    public static DomainError ContractDatesInvalid { get; } =
        new("ContractDatesInvalid", "A contract price must end on or after the day it starts.");

    /// <summary>The name is missing.</summary>
    public static DomainError NameRequired { get; } = new("NameRequired", "A name is required.");

    /// <summary>The name is too long.</summary>
    public static DomainError NameTooLong { get; } =
        new("NameTooLong", $"A name can be at most {MaxNameLength} characters.");
}
