using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>
/// A stock-keeping unit: the human-facing code for an offering, such as <c>ACM-BRG-6204</c>.
/// Stored upper-case; input is trimmed and upper-cased first, so <c>acm-brg-6204</c> is the same SKU.
/// </summary>
public readonly record struct Sku
{
    private Sku(string value) => Value = value;

    /// <summary>The SKU, upper-case.</summary>
    public string Value { get; }

    /// <summary>Normalises and validates a SKU: 2–32 letters, digits or hyphens, starting with a letter or digit.</summary>
    public static Result<Sku> Create(string? input)
    {
        var value = input?.Trim().ToUpperInvariant() ?? string.Empty;
        var valid = value.Length is >= 2 and <= 32
            && char.IsAsciiLetterOrDigit(value[0])
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

        return valid ? Result.Success(new Sku(value)) : Result.Failure<Sku>(CatalogErrors.SkuInvalid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
