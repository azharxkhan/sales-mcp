using System.Globalization;
using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Quotes;

/// <summary>
/// The number printed on a quote, such as <c>Q-2026-000123</c>: the year and a six-digit sequence.
/// The sequence itself is issued by the data layer.
/// </summary>
public readonly record struct QuoteNumber
{
    private const int MaxSequence = 999_999;

    private QuoteNumber(string value) => Value = value;

    /// <summary>The formatted number.</summary>
    public string Value { get; }

    /// <summary>Builds a number from a year and a sequence (1–999999).</summary>
    public static Result<QuoteNumber> From(int year, int sequence) =>
        year is >= 2000 and <= 9999 && sequence is >= 1 and <= MaxSequence
            ? Result.Success(new QuoteNumber(string.Create(CultureInfo.InvariantCulture, $"Q-{year:D4}-{sequence:D6}")))
            : Result.Failure<QuoteNumber>(QuoteErrors.NumberInvalid);

    /// <summary>Parses a number such as <c>Q-2026-000123</c>. Case and surrounding spaces are ignored.</summary>
    public static Result<QuoteNumber> Parse(string? input)
    {
        var value = input?.Trim().ToUpperInvariant() ?? string.Empty;
        var shaped = value.Length == 13
            && value.StartsWith("Q-", StringComparison.Ordinal)
            && value[6] == '-'
            && value[2..6].All(char.IsAsciiDigit)
            && value[7..].All(char.IsAsciiDigit);

        return shaped
            ? From(int.Parse(value[2..6], CultureInfo.InvariantCulture), int.Parse(value[7..], CultureInfo.InvariantCulture))
            : Result.Failure<QuoteNumber>(QuoteErrors.NumberInvalid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
