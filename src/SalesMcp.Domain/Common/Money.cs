using System.Globalization;

namespace SalesMcp.Domain.Common;

/// <summary>
/// An amount of money in one currency. Arithmetic between different currencies is a bug and throws.
/// Amounts keep full <see cref="decimal"/> precision until <see cref="Round"/> is called, which
/// happens per line (ADR-0007).
/// </summary>
public readonly record struct Money
{
    /// <summary>Number of decimal places money is rounded to.</summary>
    public const int Decimals = 2;

    /// <summary>Creates an amount in the given ISO 4217 currency (three upper-case letters).</summary>
    /// <param name="amount">The amount. Negative amounts are allowed (credits, discounts).</param>
    /// <param name="currency">ISO 4217 code, for example <c>ZAR</c>.</param>
    /// <exception cref="ArgumentException">The currency code isn't three upper-case letters.</exception>
    public Money(decimal amount, string currency)
    {
        if (!IsValidCurrency(currency))
        {
            throw new ArgumentException($"'{currency}' is not an ISO 4217 currency code (three upper-case letters).", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    /// <summary>The amount.</summary>
    public decimal Amount { get; }

    /// <summary>The ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>True when the amount is exactly zero.</summary>
    public bool IsZero => Amount == 0m;

    /// <summary>True when the amount is below zero.</summary>
    public bool IsNegative => Amount < 0m;

    /// <summary>Zero in the given currency.</summary>
    public static Money Zero(string currency) => new(0m, currency);

    /// <summary>Rounds to <see cref="Decimals"/> places, midpoint away from zero (0.005 becomes 0.01).</summary>
    public Money Round() => new(decimal.Round(Amount, Decimals, MidpointRounding.AwayFromZero), Currency);

    /// <summary>Adds two amounts in the same currency.</summary>
    public Money Add(Money other) => new(Amount + SameCurrency(other).Amount, Currency);

    /// <summary>Subtracts an amount in the same currency.</summary>
    public Money Subtract(Money other) => new(Amount - SameCurrency(other).Amount, Currency);

    /// <summary>Multiplies by a factor, for example a quantity or a rate.</summary>
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>The negated amount.</summary>
    public Money Negate() => new(-Amount, Currency);

    /// <summary>Adds two amounts in the same currency.</summary>
    public static Money operator +(Money left, Money right) => left.Add(right);

    /// <summary>Subtracts an amount in the same currency.</summary>
    public static Money operator -(Money left, Money right) => left.Subtract(right);

    /// <summary>Multiplies by a factor.</summary>
    public static Money operator *(Money money, decimal factor) => money.Multiply(factor);

    /// <summary>Multiplies by a factor.</summary>
    public static Money operator *(decimal factor, Money money) => money.Multiply(factor);

    /// <summary>The negated amount.</summary>
    public static Money operator -(Money money) => money.Negate();

    /// <summary>Sums amounts that all share <paramref name="currency"/>. An empty sequence sums to zero.</summary>
    public static Money Sum(IEnumerable<Money> amounts, string currency)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        return amounts.Aggregate(Zero(currency), (total, next) => total + next);
    }

    /// <summary>Invariant text such as <c>ZAR 1234.50</c>, for logs and tests. Not for display to buyers.</summary>
    public override string ToString() =>
        $"{Currency} {Amount.ToString("0.00##########", CultureInfo.InvariantCulture)}";

    private Money SameCurrency(Money other) =>
        string.Equals(Currency, other.Currency, StringComparison.Ordinal)
            ? other
            : throw new InvalidOperationException($"Currency mismatch: {Currency} and {other.Currency}.");

    private static bool IsValidCurrency(string? code) =>
        code is { Length: 3 } && code.All(c => c is >= 'A' and <= 'Z');
}
