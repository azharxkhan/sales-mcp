using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Quotes;

/// <summary>Business-rule failures for quotes and orders. Codes are stable; tools and tests match on them.</summary>
public static class QuoteErrors
{
    /// <summary>Default number of days a quote stays open.</summary>
    public const int DefaultValidDays = 30;

    /// <summary>Longest a quote may stay open.</summary>
    public const int MaxValidDays = 90;

    /// <summary>The quote number doesn't match the format.</summary>
    public static DomainError NumberInvalid { get; } =
        new("QuoteNumberInvalid", "A quote number looks like Q-2026-000123.");

    /// <summary>The quote has no lines.</summary>
    public static DomainError Empty { get; } = new("QuoteEmpty", "A quote needs at least one line.");

    /// <summary>A line's price was worked out for another customer.</summary>
    public static DomainError CustomerMismatch { get; } =
        new("QuoteCustomerMismatch", "Every line must be priced for the quote's customer.");

    /// <summary>The seller isn't the business the customer buys from.</summary>
    public static DomainError SellerMismatch { get; } =
        new("SellerMismatch", "A quote must come from the customer's business.");

    /// <summary>A line's offering belongs to another business.</summary>
    public static DomainError BusinessMismatch { get; } =
        new("BusinessMismatch", "Every line must be for an item sold by the customer's business.");

    /// <summary>A line's price is for a different offering.</summary>
    public static DomainError LineOfferingMismatch { get; } =
        new("LineOfferingMismatch", "A line's price must be for that line's item.");

    /// <summary>The same offering is on two lines.</summary>
    public static DomainError LineDuplicate { get; } =
        new("LineDuplicate", "Each item can appear on only one line. Combine the quantities so the right price break applies.");

    /// <summary>Lines are in different currencies.</summary>
    public static DomainError CurrencyMismatch { get; } =
        new("CurrencyMismatch", "All lines on a quote must be in one currency.");

    /// <summary>The validity period is out of range.</summary>
    public static DomainError ValidityInvalid { get; } =
        new("QuoteValidityInvalid", $"A quote must be valid for 1 to {MaxValidDays} days.");

    /// <summary>An enum value isn't one of the defined options.</summary>
    public static DomainError OptionUnknown { get; } =
        new("OptionUnknown", "One of the chosen options isn't recognised.");

    /// <summary>The quote has passed its valid-until date.</summary>
    public static DomainError Expired { get; } =
        new("QuoteExpired", "This quote has expired. Ask for a new quote at today's prices.");

    /// <summary>The quote has already been accepted, declined or withdrawn.</summary>
    public static DomainError NotOpen { get; } =
        new("QuoteNotOpen", "This quote is no longer open.");
}
