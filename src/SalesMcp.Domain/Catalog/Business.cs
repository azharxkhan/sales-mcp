using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>
/// A seller with its own catalog, customers and agents, such as the demo business Acme Distributors.
/// Its currency is the currency of every price it sets.
/// </summary>
public sealed class Business
{
    private Business(BusinessId id, string name, string currency, string locale)
    {
        Id = id;
        Name = name;
        Currency = currency;
        Locale = locale;
    }

    /// <summary>The business's id.</summary>
    public BusinessId Id { get; }

    /// <summary>Trading name.</summary>
    public string Name { get; }

    /// <summary>ISO 4217 currency for all of its prices, for example ZAR.</summary>
    public string Currency { get; }

    /// <summary>Language-region tag used for formatting, for example en-ZA. Formatting only, never rules.</summary>
    public string Locale { get; }

    /// <summary>Validates and creates a business.</summary>
    public static Result<Business> Create(BusinessId id, string? name, string? currency, string? locale)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<Business>(CatalogErrors.NameRequired);
        }

        if (trimmedName.Length > CatalogErrors.MaxNameLength)
        {
            return Result.Failure<Business>(CatalogErrors.NameTooLong);
        }

        if (!Money.IsValidCurrency(currency))
        {
            return Result.Failure<Business>(CatalogErrors.CurrencyInvalid);
        }

        if (!IsLanguageRegionTag(locale))
        {
            return Result.Failure<Business>(CatalogErrors.LocaleInvalid);
        }

        return Result.Success(new Business(id, trimmedName, currency!, locale!));
    }

    private static bool IsLanguageRegionTag(string? tag) =>
        tag is { Length: 5 }
        && char.IsAsciiLetterLower(tag[0]) && char.IsAsciiLetterLower(tag[1])
        && tag[2] == '-'
        && char.IsAsciiLetterUpper(tag[3]) && char.IsAsciiLetterUpper(tag[4]);
}
