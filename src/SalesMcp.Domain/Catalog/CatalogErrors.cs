using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>Business-rule failures for the catalog. Codes are stable; tools and tests match on them.</summary>
public static class CatalogErrors
{
    /// <summary>Longest allowed offering or business name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>Longest category name.</summary>
    public const int MaxCategoryLength = 60;

    /// <summary>
    /// Longest allowed offering description. Descriptions are untrusted text that will be shown to a
    /// model, so they are capped here and sanitised on output (guardrail D4).
    /// </summary>
    public const int MaxDescriptionLength = 2000;

    /// <summary>Longest allowed reason on an offering rule.</summary>
    public const int MaxReasonLength = 500;

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

    /// <summary>The category name is too long.</summary>
    public static DomainError CategoryTooLong { get; } =
        new("CategoryTooLong", $"A category can be at most {MaxCategoryLength} characters.");

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

    /// <summary>An offering rule points at the offering it belongs to.</summary>
    public static DomainError RuleTargetsItself { get; } =
        new("RuleTargetsItself", "An offering rule can't refer to the offering it belongs to.");

    /// <summary>An offering rule has no reason.</summary>
    public static DomainError ReasonRequired { get; } =
        new("ReasonRequired", "An offering rule needs a reason the agent can explain to the buyer.");

    /// <summary>An offering rule's reason is too long.</summary>
    public static DomainError ReasonTooLong { get; } =
        new("ReasonTooLong", $"A reason can be at most {MaxReasonLength} characters.");

    /// <summary>The same rule already exists.</summary>
    public static DomainError RuleDuplicate { get; } =
        new("RuleDuplicate", "That rule already exists.");

    /// <summary>A different rule already links the same two offerings.</summary>
    public static DomainError RuleConflict { get; } =
        new("RuleConflict", "A different rule already links these two offerings.");
}
