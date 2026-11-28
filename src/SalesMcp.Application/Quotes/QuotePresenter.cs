using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Application.Quotes;

/// <summary>
/// Prepares a stored quote for showing: cleans its untrusted text and renders its document. Used for
/// a new quote, a retried one and the quote resource, so all three show the same document. It reads
/// only the quote itself, so later changes to the business, customer or catalog can't alter the offer
/// (ADR-0015).
/// </summary>
public static class QuotePresenter
{
    /// <summary>Longest step detail returned.</summary>
    public const int MaxDetailLength = 300;

    /// <summary>Presents <paramref name="quote"/>.</summary>
    public static IssuedQuote Present(Quote quote)
    {
        ArgumentNullException.ThrowIfNull(quote);

        var lines = quote.Lines
            .OrderBy(l => l.LineNumber)
            .Select(line => new IssuedQuoteLine(
                line,
                UntrustedText.Clean(line.Name, CatalogErrors.MaxNameLength),
                [.. line.Price.Steps.Select(s => s with { Detail = UntrustedText.Clean(s.Detail, MaxDetailLength) })]))
            .ToList();

        var issued = new IssuedQuote(quote, lines, string.Empty);
        return issued with { Document = QuoteDocument.Render(issued) };
    }
}
