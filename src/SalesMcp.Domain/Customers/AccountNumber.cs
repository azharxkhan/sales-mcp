using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>
/// The human-facing code for a customer account, such as <c>BLD-0042</c>. Stored upper-case;
/// input is trimmed and upper-cased first.
/// </summary>
public readonly record struct AccountNumber
{
    private AccountNumber(string value) => Value = value;

    /// <summary>The account number, upper-case.</summary>
    public string Value { get; }

    /// <summary>Normalises and validates an account number: 2–20 letters, digits or hyphens, starting with a letter or digit.</summary>
    public static Result<AccountNumber> Create(string? input)
    {
        var value = input?.Trim().ToUpperInvariant() ?? string.Empty;
        var valid = value.Length is >= 2 and <= 20
            && char.IsAsciiLetterOrDigit(value[0])
            && value.All(c => char.IsAsciiLetterOrDigit(c) || c == '-');

        return valid
            ? Result.Success(new AccountNumber(value))
            : Result.Failure<AccountNumber>(CustomerErrors.AccountNumberInvalid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
