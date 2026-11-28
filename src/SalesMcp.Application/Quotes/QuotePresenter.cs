using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Application.Quotes;

/// <summary>
/// Prepares a stored quote for showing: cleans its untrusted text and renders its document. Used for
/// a new quote, a retried one and the quote resource, so all three show the same document.
/// </summary>
/// <param name="businesses">Reads the seller's name.</param>
/// <param name="catalog">Reads offerings for their units.</param>
public sealed class QuotePresenter(IBusinessReader businesses, ICatalogReader catalog)
{
    /// <summary>Longest step detail returned.</summary>
    public const int MaxDetailLength = 300;

    /// <summary>Presents <paramref name="quote"/>, which belongs to <paramref name="customer"/>.</summary>
    public async Task<IssuedQuote> PresentAsync(Quote quote, Customer customer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(quote);
        ArgumentNullException.ThrowIfNull(customer);

        var business = await businesses.GetBusinessAsync(customer.BusinessId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("The session's business is missing.");

        var lines = new List<IssuedQuoteLine>(quote.Lines.Count);
        foreach (var line in quote.Lines.OrderBy(l => l.LineNumber))
        {
            var offering = await catalog.GetOfferingAsync(line.OfferingId, cancellationToken).ConfigureAwait(false);
            lines.Add(new IssuedQuoteLine(
                line,
                UntrustedText.Clean(line.Name, CatalogErrors.MaxNameLength),
                (offering as Product)?.Unit,
                [.. line.Price.Steps.Select(s => s with { Detail = UntrustedText.Clean(s.Detail, MaxDetailLength) })]));
        }

        var issued = new IssuedQuote(
            quote,
            business.Name,
            customer.Name,
            customer.AccountNumber,
            customer.PaymentTerms,
            lines,
            string.Empty);
        return issued with { Document = QuoteDocument.Render(issued) };
    }
}
