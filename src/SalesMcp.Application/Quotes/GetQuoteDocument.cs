using System.Globalization;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Application.Quotes;

/// <summary>Errors from <see cref="GetQuoteDocument"/>.</summary>
public static class QuoteQueryErrors
{
    /// <summary>The customer has no such quote (or it is another customer's).</summary>
    public static readonly DomainError QuoteNotFound = new("QuoteNotFound", "No such quote was found on this account.");
}

/// <summary>
/// The document of one of the session's customer's quotes. Another customer's quote is "not found",
/// so ids and numbers can't be probed (guardrail D2). A quote that can no longer be accepted says so
/// at the top, so an old document isn't mistaken for a live offer.
/// </summary>
/// <param name="quotes">Reads quotes.</param>
/// <param name="customers">Reads the session's customer.</param>
/// <param name="presenter">Renders the document.</param>
/// <param name="time">The clock for today.</param>
public sealed class GetQuoteDocument(IQuoteRepository quotes, ICustomerReader customers, QuotePresenter presenter, TimeProvider time)
{
    /// <summary>The document of <paramref name="quote"/>, a quote id or number.</summary>
    /// <param name="customerId">The session's customer. Never a model argument.</param>
    /// <param name="quote">A quote id or number, unchecked.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task<Result<string>> ExecuteAsync(CustomerId customerId, string? quote, CancellationToken cancellationToken)
    {
        var found = await FindAsync(customerId, quote?.Trim() ?? string.Empty, cancellationToken).ConfigureAwait(false);
        if (found is null || found.CustomerId != customerId)
        {
            return Result.Failure<string>(QuoteQueryErrors.QuoteNotFound);
        }

        var customer = await customers.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return Result.Failure<string>(QuoteQueryErrors.QuoteNotFound);
        }

        var issued = await presenter.PresentAsync(found, customer, cancellationToken).ConfigureAwait(false);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(time.GetUtcNow(), time.LocalTimeZone).DateTime);
        return Result.Success(Notice(found, today) + issued.Document);
    }

    private static string Notice(Quote quote, DateOnly today)
    {
        var reason = quote.Status switch
        {
            QuoteStatus.Accepted => "it has been accepted and is now an order",
            QuoteStatus.Declined => "it was declined",
            QuoteStatus.Withdrawn => "it was withdrawn",
            _ when quote.IsExpired(today) => string.Create(
                CultureInfo.InvariantCulture, $"it expired after {quote.ValidUntil:yyyy-MM-dd}"),
            _ => null,
        };

        return reason is null ? string.Empty : $"> **This quote is no longer open for acceptance: {reason}.**\n\n";
    }

    private async Task<Quote?> FindAsync(CustomerId customerId, string reference, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(reference, out var id))
        {
            return await quotes.GetAsync(new QuoteId(id), cancellationToken).ConfigureAwait(false);
        }

        var number = QuoteNumber.Parse(reference);
        return number.IsSuccess
            ? await quotes.FindByNumberAsync(customerId, number.Value, cancellationToken).ConfigureAwait(false)
            : null;
    }
}
