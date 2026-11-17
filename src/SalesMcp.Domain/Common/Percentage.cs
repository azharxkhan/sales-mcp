using System.Globalization;

namespace SalesMcp.Domain.Common;

/// <summary>A percentage from 0 to 100 inclusive, for discounts and rates.</summary>
public readonly record struct Percentage : IComparable<Percentage>
{
    /// <summary>Error code for a value outside 0–100.</summary>
    public const string OutOfRangeCode = "PercentageOutOfRange";

    /// <summary>0%.</summary>
    public static readonly Percentage Zero = new(0m);

    /// <summary>Creates a percentage. Use <see cref="Create"/> for values that come from input.</summary>
    /// <param name="value">From 0 to 100, for example <c>15</c> for 15%.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside 0–100.</exception>
    public Percentage(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 100m);
        Value = value;
    }

    /// <summary>The percentage, from 0 to 100.</summary>
    public decimal Value { get; }

    /// <summary>The percentage as a fraction, for example 0.15 for 15%.</summary>
    public decimal AsFraction => Value / 100m;

    /// <summary>Validates input and creates a percentage, or returns <see cref="OutOfRangeCode"/>.</summary>
    public static Result<Percentage> Create(decimal value) =>
        value is >= 0m and <= 100m
            ? Result.Success(new Percentage(value))
            : Result.Failure<Percentage>(OutOfRangeCode, "A percentage must be between 0 and 100.");

    /// <summary>This percentage of an amount, unrounded. 15% of R100 is R15.</summary>
    public Money Of(Money amount) => amount * AsFraction;

    /// <inheritdoc />
    public int CompareTo(Percentage other) => Value.CompareTo(other.Value);

    /// <summary>Compares two percentages.</summary>
    public static bool operator <(Percentage left, Percentage right) => left.CompareTo(right) < 0;

    /// <summary>Compares two percentages.</summary>
    public static bool operator >(Percentage left, Percentage right) => left.CompareTo(right) > 0;

    /// <summary>Compares two percentages.</summary>
    public static bool operator <=(Percentage left, Percentage right) => left.CompareTo(right) <= 0;

    /// <summary>Compares two percentages.</summary>
    public static bool operator >=(Percentage left, Percentage right) => left.CompareTo(right) >= 0;

    /// <summary>Invariant text such as <c>15%</c>.</summary>
    public override string ToString() => Value.ToString("0.##", CultureInfo.InvariantCulture) + "%";
}
