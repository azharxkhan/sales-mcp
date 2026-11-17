using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>Business-rule failures for the catalog. Codes are stable; tools and tests match on them.</summary>
public static class CatalogErrors
{
    /// <summary>Longest allowed offering or business name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>
    /// Longest allowed offering description. Descriptions are untrusted text that will be shown to a
    /// model, so they are capped here and sanitised on output (guardrail D4).
    /// </summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>The SKU doesn't match the allowed format.</summary>
    public static DomainError SkuInvalid { get; } =
        new("SkuInvalid", "A SKU must be 2 to 32 characters: letters, digits and hyphens, starting with a letter or digit.");

    /// <summary>The name is missing.</summary>
    public static DomainError NameRequired { get; } = new("NameRequired", "A name is required.");

    /// <summary>The name is too long.</summary>
    public static DomainError NameTooLong { get; } =
        new("NameTooLong", $"A name can be at most {MaxNameLength} characters.");

    /// <summary>The description is too long.</summary>
    public static DomainError DescriptionTooLong { get; } =
        new("DescriptionTooLong", $"A description can be at most {MaxDescriptionLength} characters.");

    /// <summary>A day count is negative.</summary>
    public static DomainError DaysNegative { get; } =
        new("DaysNegative", "Lead times and return windows can't be negative.");

    /// <summary>The currency isn't a three-letter ISO 4217 code.</summary>
    public static DomainError CurrencyInvalid { get; } =
        new("CurrencyInvalid", "The currency must be a three-letter ISO 4217 code such as ZAR.");

    /// <summary>The locale isn't a language-region tag such as en-ZA.</summary>
    public static DomainError LocaleInvalid { get; } =
        new("LocaleInvalid", "The locale must be a language-region tag such as en-ZA.");

    /// <summary>An enum value isn't one of the defined options.</summary>
    public static DomainError OptionUnknown { get; } =
        new("OptionUnknown", "One of the chosen options isn't recognised.");
}
