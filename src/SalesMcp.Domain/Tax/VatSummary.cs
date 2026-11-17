using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tax;

/// <summary>
/// Totals for a document (quote or order) with VAT shown per code, as quote documents must
/// disclose (guardrail E3). Totals are sums of the already-rounded lines, so the document adds up.
/// </summary>
public sealed class VatSummary
{
    private VatSummary(string currency, IReadOnlyList<VatCodeTotal> byCode, Money net, Money vat)
    {
        Currency = currency;
        ByCode = byCode;
        Net = net;
        Vat = vat;
    }

    /// <summary>The document's currency.</summary>
    public string Currency { get; }

    /// <summary>One entry per VAT code that appears on the document, in <see cref="VatCode"/> order.</summary>
    public IReadOnlyList<VatCodeTotal> ByCode { get; }

    /// <summary>Sum of line nets.</summary>
    public Money Net { get; }

    /// <summary>Sum of line VAT.</summary>
    public Money Vat { get; }

    /// <summary>Net plus VAT.</summary>
    public Money Total => Net + Vat;

    /// <summary>Summarises lines that all share <paramref name="currency"/>.</summary>
    /// <exception cref="InvalidOperationException">A line is in another currency.</exception>
    public static VatSummary Of(IEnumerable<VatLine> lines, string currency)
    {
        ArgumentNullException.ThrowIfNull(lines);
        var list = lines.ToList();

        var byCode = list
            .GroupBy(line => line.Code)
            .OrderBy(group => group.Key)
            .Select(group => new VatCodeTotal(
                group.Key,
                group.First().Rate,
                Money.Sum(group.Select(l => l.Net), currency),
                Money.Sum(group.Select(l => l.Vat), currency)))
            .ToList();

        return new VatSummary(
            currency,
            byCode,
            Money.Sum(list.Select(l => l.Net), currency),
            Money.Sum(list.Select(l => l.Vat), currency));
    }
}

/// <summary>Net and VAT totals for one VAT code on a document.</summary>
/// <param name="Code">The VAT code.</param>
/// <param name="Rate">The rate, or <see langword="null"/> when exempt.</param>
/// <param name="Net">Sum of line nets with this code.</param>
/// <param name="Vat">Sum of line VAT with this code.</param>
public sealed record VatCodeTotal(VatCode Code, Percentage? Rate, Money Net, Money Vat);
