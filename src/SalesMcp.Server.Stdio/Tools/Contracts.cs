using SalesMcp.Domain.Common;

namespace SalesMcp.Server.Stdio.Tools;

/// <summary>An amount of money in a tool result: <c>{ "amount": 1086.75, "currency": "ZAR" }</c>.</summary>
/// <param name="Amount">The amount, in the currency's major unit.</param>
/// <param name="Currency">The ISO 4217 currency code.</param>
public sealed record MoneyAmount(decimal Amount, string Currency)
{
    /// <summary>Maps a domain amount.</summary>
    public static MoneyAmount From(Money money) => new(money.Amount, money.Currency);
}

/// <summary>How tool results spell domain values, so every tool spells them the same way.</summary>
public static class Wire
{
    /// <summary>
    /// An enum value in camelCase, for example <c>DaysFromEndOfMonth</c> as <c>daysFromEndOfMonth</c>.
    /// Enum names are ASCII, so lower-casing the first letter is enough and avoids culture rules.
    /// </summary>
    public static string Name<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
