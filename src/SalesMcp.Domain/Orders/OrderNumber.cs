using System.Globalization;
using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Orders;

/// <summary>
/// The number printed on an order and quoted by the buyer, such as <c>SO-2026-000123</c>: the year
/// and a six-digit sequence. The sequence itself is issued by the data layer.
/// </summary>
public readonly record struct OrderNumber
{
    /// <summary>Length of a formatted number.</summary>
    public const int Length = 14;

    private const int MaxSequence = 999_999;

    private OrderNumber(string value) => Value = value;

    /// <summary>The formatted number.</summary>
    public string Value { get; }

    /// <summary>Builds a number from a year and a sequence (1–999999).</summary>
    public static Result<OrderNumber> From(int year, int sequence) =>
        year is >= 2000 and <= 9999 && sequence is >= 1 and <= MaxSequence
            ? Result.Success(new OrderNumber(string.Create(CultureInfo.InvariantCulture, $"SO-{year:D4}-{sequence:D6}")))
            : Result.Failure<OrderNumber>(OrderErrors.NumberInvalid);

    /// <summary>Parses a number such as <c>SO-2026-000123</c>. Case and surrounding spaces are ignored.</summary>
    public static Result<OrderNumber> Parse(string? input)
    {
        var value = input?.Trim().ToUpperInvariant() ?? string.Empty;
        var shaped = value.Length == Length
            && value.StartsWith("SO-", StringComparison.Ordinal)
            && value[7] == '-'
            && value[3..7].All(char.IsAsciiDigit)
            && value[8..].All(char.IsAsciiDigit);

        return shaped
            ? From(int.Parse(value[3..7], CultureInfo.InvariantCulture), int.Parse(value[8..], CultureInfo.InvariantCulture))
            : Result.Failure<OrderNumber>(OrderErrors.NumberInvalid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
