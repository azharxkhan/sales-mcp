using System.Globalization;

namespace SalesMcp.Domain.Common;

/// <summary>
/// A quantity greater than zero, in the offering's unit of measure (the unit lives on the offering).
/// Decimal so units such as metres or litres work.
/// </summary>
public readonly record struct Quantity : IComparable<Quantity>
{
    /// <summary>Error code for zero or negative quantities.</summary>
    public const string NotPositiveCode = "QuantityNotPositive";

    /// <summary>Creates a quantity. Use <see cref="Create"/> for values that come from input.</summary>
    /// <param name="value">Greater than zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The value is zero or negative.</exception>
    public Quantity(decimal value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        Value = value;
    }

    /// <summary>The quantity.</summary>
    public decimal Value { get; }

    /// <summary>Validates input and creates a quantity, or returns <see cref="NotPositiveCode"/>.</summary>
    public static Result<Quantity> Create(decimal value) =>
        value > 0m
            ? Result.Success(new Quantity(value))
            : Result.Failure<Quantity>(NotPositiveCode, "The quantity must be greater than zero.");

    /// <inheritdoc />
    public int CompareTo(Quantity other) => Value.CompareTo(other.Value);

    /// <summary>Compares two quantities.</summary>
    public static bool operator <(Quantity left, Quantity right) => left.CompareTo(right) < 0;

    /// <summary>Compares two quantities.</summary>
    public static bool operator >(Quantity left, Quantity right) => left.CompareTo(right) > 0;

    /// <summary>Compares two quantities.</summary>
    public static bool operator <=(Quantity left, Quantity right) => left.CompareTo(right) <= 0;

    /// <summary>Compares two quantities.</summary>
    public static bool operator >=(Quantity left, Quantity right) => left.CompareTo(right) >= 0;

    /// <inheritdoc />
    public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
}
