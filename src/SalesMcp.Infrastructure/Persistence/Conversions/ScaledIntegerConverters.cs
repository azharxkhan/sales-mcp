using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SalesMcp.Domain.Common;

namespace SalesMcp.Infrastructure.Persistence.Conversions;

/// <summary>
/// SQLite has no decimal type. EF Core stores decimals there as text and sorts and compares them with
/// a collation that parses the text using the current culture, which fails on machines whose decimal
/// separator is a comma (en-ZA, our home market). So on SQLite every decimal is stored as a scaled
/// integer: exact, culture-free, and sorted, compared and summed natively by SQLite.
/// </summary>
/// <remarks>
/// A value with more decimals than the scale allows is rejected rather than silently rounded, so
/// nothing is ever stored as anything other than what the domain holds.
/// </remarks>
internal static class ScaledInteger
{
    /// <summary>Decimals kept for money and stock amounts.</summary>
    public const int AmountScale = 4;

    /// <summary>Decimals kept for percentages.</summary>
    public const int PercentageScale = 2;

    public static long ToStored(decimal value, int scale)
    {
        var factor = Factor(scale);
        var scaled = value * factor;
        if (scaled != decimal.Truncate(scaled))
        {
            throw new InvalidOperationException(
                $"{value} has more than {scale} decimal places and can't be stored without rounding.");
        }

        return decimal.ToInt64(scaled);
    }

    public static decimal FromStored(long value, int scale) => value / Factor(scale);

    private static decimal Factor(int scale) => scale switch
    {
        AmountScale => 10_000m,
        PercentageScale => 100m,
        _ => throw new ArgumentOutOfRangeException(nameof(scale), scale, "Unsupported scale."),
    };
}

/// <summary>Money and stock amounts as integer ten-thousandths (ZAR 12.3456 is stored as 123456).</summary>
internal sealed class ScaledDecimalConverter() : ValueConverter<decimal, long>(
    v => ScaledInteger.ToStored(v, ScaledInteger.AmountScale),
    v => ScaledInteger.FromStored(v, ScaledInteger.AmountScale));

/// <summary>Quantities as integer ten-thousandths, so 2.5 metres is stored as 25000.</summary>
internal sealed class ScaledQuantityConverter() : ValueConverter<Quantity, long>(
    v => ScaledInteger.ToStored(v.Value, ScaledInteger.AmountScale),
    v => new Quantity(ScaledInteger.FromStored(v, ScaledInteger.AmountScale)));

/// <summary>Percentages as integer hundredths, so 12.5% is stored as 1250.</summary>
internal sealed class ScaledPercentageConverter() : ValueConverter<Percentage, long>(
    v => ScaledInteger.ToStored(v.Value, ScaledInteger.PercentageScale),
    v => new Percentage(ScaledInteger.FromStored(v, ScaledInteger.PercentageScale)));
