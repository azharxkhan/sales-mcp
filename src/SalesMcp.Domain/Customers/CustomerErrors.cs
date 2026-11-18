using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>Business-rule failures for customers and contacts. Codes are stable; tools and tests match on them.</summary>
public static class CustomerErrors
{
    /// <summary>Longest allowed customer or contact name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Longest allowed email address (RFC 5321 path limit).</summary>
    public const int MaxEmailLength = 254;

    /// <summary>Longest allowed payment term, in days.</summary>
    public const int MaxPaymentDays = 120;

    /// <summary>The account number doesn't match the allowed format.</summary>
    public static DomainError AccountNumberInvalid { get; } =
        new("AccountNumberInvalid", "An account number must be 2 to 20 characters: letters, digits and hyphens, starting with a letter or digit.");

    /// <summary>The VAT number isn't a South African VAT number.</summary>
    public static DomainError VatNumberInvalid { get; } =
        new("VatNumberInvalid", "A VAT number must be 10 digits starting with 4.");

    /// <summary>The name is missing.</summary>
    public static DomainError NameRequired { get; } = new("NameRequired", "A name is required.");

    /// <summary>The name is too long.</summary>
    public static DomainError NameTooLong { get; } =
        new("NameTooLong", $"A name can be at most {MaxNameLength} characters.");

    /// <summary>The email address isn't usable.</summary>
    public static DomainError EmailInvalid { get; } =
        new("EmailInvalid", "The email address isn't valid.");

    /// <summary>The credit limit is negative.</summary>
    public static DomainError CreditLimitNegative { get; } =
        new("CreditLimitNegative", "A credit limit can't be negative.");

    /// <summary>The credit limit and balance are in different currencies.</summary>
    public static DomainError CurrencyMismatch { get; } =
        new("CurrencyMismatch", "The credit limit and balance must be in the same currency.");

    /// <summary>The money amounts have no valid currency.</summary>
    public static DomainError CurrencyInvalid { get; } =
        new("CurrencyInvalid", "The currency must be a three-letter ISO 4217 code such as ZAR.");

    /// <summary>The payment term's days don't suit its basis.</summary>
    public static DomainError PaymentDaysInvalid { get; } =
        new("PaymentDaysInvalid", $"Cash with order has 0 days; other terms need 1 to {MaxPaymentDays} days.");

    /// <summary>The consent record has no time.</summary>
    public static DomainError ConsentTimeRequired { get; } =
        new("ConsentTimeRequired", "A consent record needs the time it was given or withdrawn.");

    /// <summary>An enum value isn't one of the defined options.</summary>
    public static DomainError OptionUnknown { get; } =
        new("OptionUnknown", "One of the chosen options isn't recognised.");
}
