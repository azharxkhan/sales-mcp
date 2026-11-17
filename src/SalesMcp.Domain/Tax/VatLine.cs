using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tax;

/// <summary>
/// The VAT result for one line: rounded net, rounded VAT and their total.
/// Created by <see cref="VatRateTable.Calculate"/>.
/// </summary>
/// <param name="Code">The line's VAT code.</param>
/// <param name="Rate">The rate applied, or <see langword="null"/> when exempt.</param>
/// <param name="Net">The line amount after discount, rounded.</param>
/// <param name="Vat">The VAT, rounded. Zero when zero-rated or exempt.</param>
/// <param name="Total">Net plus VAT.</param>
public sealed record VatLine(VatCode Code, Percentage? Rate, Money Net, Money Vat, Money Total)
{
    /// <summary>True when the line isn't subject to VAT at all (as opposed to zero-rated).</summary>
    public bool IsExempt => Code == VatCode.Exempt;
}
