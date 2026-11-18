using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>
/// A South African VAT registration number: 10 digits starting with 4. A full tax invoice must show
/// the recipient's VAT number when the recipient is registered.
/// </summary>
public readonly record struct VatNumber
{
    private VatNumber(string value) => Value = value;

    /// <summary>The 10-digit number.</summary>
    public string Value { get; }

    /// <summary>Validates a VAT number. Spaces are removed first, so <c>4123 456 789</c> is accepted.</summary>
    public static Result<VatNumber> Create(string? input)
    {
        var value = input?.Replace(" ", string.Empty, StringComparison.Ordinal) ?? string.Empty;
        var valid = value.Length == 10 && value[0] == '4' && value.All(char.IsAsciiDigit);

        return valid ? Result.Success(new VatNumber(value)) : Result.Failure<VatNumber>(CustomerErrors.VatNumberInvalid);
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
