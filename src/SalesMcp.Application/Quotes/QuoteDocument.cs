using System.Globalization;
using System.Text;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Application.Quotes;

/// <summary>
/// Renders a quote as the document the buyer reads: its number, lines, totals with VAT per code,
/// validity and terms, and the statement that this document is the offer (guardrail A2, ADR-0015).
/// </summary>
public static class QuoteDocument
{
    /// <summary>The sentence that makes the document, not the chat, the offer.</summary>
    public const string OfferStatement =
        "This quote is the offer. Only the prices, quantities and terms in this document are binding; nothing said in chat changes, adds to or replaces them.";

    /// <summary>Renders <paramref name="quote"/> as Markdown.</summary>
    /// <param name="quote">The issued quote, with its lines cleaned for display.</param>
    public static string Render(IssuedQuote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);
        var q = quote.Quote;
        var text = new StringBuilder();

        Line(text, $"# Quote {q.Number}");
        Line(text);
        Line(text, $"**From:** {Cell(quote.BusinessName)}  ");
        Line(text, $"**To:** {Cell(quote.CustomerName)} (account {quote.AccountNumber})  ");
        Line(text, $"**Issued:** {Day(q.IssuedOn)}  ");
        Line(text, $"**Valid until:** {Day(q.ValidUntil)} (end of day)");
        Line(text);
        Line(text, "| # | SKU | Item | Quantity | Unit price | Net | VAT | Total |");
        Line(text, "|---|---|---|---:|---:|---:|---:|---:|");
        foreach (var line in quote.Lines)
        {
            var l = line.Line;
            var quantity = Number(l.Quantity.Value) + (line.Unit is { } unit ? " " + unit.ToString().ToLowerInvariant() : string.Empty);
            Line(text, $"| {l.LineNumber} | {l.Sku} | {Cell(line.Name)} | {quantity} | {Amount(l.Price.UnitPrice)} | {Amount(l.Net)} | {VatLabel(l.Price.Vat.Code, l.Price.Vat.Rate)} {Amount(l.Vat)} | {Amount(l.Total)} |");
        }

        var totals = q.Totals;
        Line(text);
        Line(text, $"**Net:** {Amount(totals.Net)}  ");
        foreach (var vat in totals.ByCode)
        {
            Line(text, $"**{VatLabel(vat.Code, vat.Rate)}** on {Amount(vat.Net)}: {Amount(vat.Vat)}  ");
        }

        Line(text, $"**Total:** {Amount(totals.Total)}");
        Line(text);
        Line(text, "## Terms");
        Line(text);
        Line(text, $"- {OfferStatement}");
        Line(text, $"- Valid until the end of {Day(q.ValidUntil)}. After that it lapses and a new quote, at the prices of that day, is needed.");
        Line(text, $"- Payment: {quote.PaymentTerms}.");
        Line(text, $"- Prices are in {q.Currency}, excluding VAT unless marked as a total. VAT is shown per line and per rate.");
        Line(text, $"- Quote {q.Number} is accepted only by confirming this exact quote; it becomes an order only then.");
        Line(text);
        Line(text, $"Issued by the AI sales agent {q.AgentId} for {Cell(quote.BusinessName)}.");
        return text.ToString();
    }

    private static void Line(StringBuilder text, string value = "") => text.Append(value).Append('\n');

    // Names come from the database: clean them and keep them from breaking the table.
    private static string Cell(string value) => UntrustedText.Clean(value, 200).Replace('|', '/');

    private static string Day(DateOnly day) => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Number(decimal value) => value.ToString("#,0.####", CultureInfo.InvariantCulture);

    private static string Amount(Money money) =>
        string.Create(CultureInfo.InvariantCulture, $"{money.Currency} {money.Amount.ToString("#,0.00##", CultureInfo.InvariantCulture)}");

    private static string VatLabel(VatCode code, Percentage? rate) => code switch
    {
        VatCode.Exempt => "VAT exempt",
        VatCode.ZeroRated => "VAT 0% (zero-rated)",
        _ => string.Create(CultureInfo.InvariantCulture, $"VAT {rate?.Value.ToString("0.##", CultureInfo.InvariantCulture)}%"),
    };
}
