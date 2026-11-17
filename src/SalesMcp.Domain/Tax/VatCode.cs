namespace SalesMcp.Domain.Tax;

/// <summary>
/// How VAT applies to an offering (ADR-0007). Every offering carries one code; the rate for each
/// code comes from a <see cref="VatRateTable"/>, so a rate change is configuration, not code.
/// </summary>
public enum VatCode
{
    /// <summary>The standard rate (15% in South Africa by default).</summary>
    Standard = 0,

    /// <summary>Taxable at 0%. VAT is charged, at a rate of zero, and shown on the document.</summary>
    ZeroRated = 1,

    /// <summary>Not subject to VAT at all. No VAT is charged, and the line is marked exempt.</summary>
    Exempt = 2,
}
