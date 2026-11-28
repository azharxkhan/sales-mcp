using System.Text.Json;
using SalesMcp.Application.Catalog;
using SalesMcp.Application.Customers;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Requests;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Application.Quotes;

/// <summary>One line a tool asked to quote, unchecked.</summary>
/// <param name="Offering">An offering id or SKU.</param>
/// <param name="Quantity">How many, in the offering's unit.</param>
public sealed record QuoteLineRequest(string? Offering, decimal Quantity);

/// <summary>A quote line ready to show: the stored line with its name and steps cleaned as untrusted text.</summary>
/// <param name="Line">The line as stored, with its price snapshot.</param>
/// <param name="Name">The name as quoted, cleaned.</param>
/// <param name="Unit">The unit the quantity is in; products only.</param>
/// <param name="Steps">The price steps, with each detail cleaned.</param>
public sealed record IssuedQuoteLine(QuoteLine Line, string Name, UnitOfMeasure? Unit, IReadOnlyList<PriceStep> Steps);

/// <summary>An issued quote with what its document needs.</summary>
/// <param name="Quote">The stored quote.</param>
/// <param name="BusinessName">The seller.</param>
/// <param name="CustomerName">The buyer.</param>
/// <param name="AccountNumber">The buyer's account number.</param>
/// <param name="PaymentTerms">The buyer's payment terms.</param>
/// <param name="Lines">The lines, in order.</param>
/// <param name="Document">The rendered document (<see cref="QuoteDocument"/>).</param>
public sealed record IssuedQuote(
    Quote Quote,
    string BusinessName,
    string CustomerName,
    AccountNumber AccountNumber,
    PaymentTerms PaymentTerms,
    IReadOnlyList<IssuedQuoteLine> Lines,
    string Document);

/// <summary>Errors from <see cref="CreateQuote"/> beyond the quote's own rules.</summary>
public static class CreateQuoteErrors
{
    /// <summary>Most lines one quote takes.</summary>
    public const int MaxLines = 50;

    /// <summary>Too many lines.</summary>
    public static readonly DomainError TooManyLines = new(
        "QuoteTooManyLines", $"A quote can have at most {MaxLines} lines.");

    /// <summary><paramref name="error"/>, with the line it is about, keeping its code.</summary>
    public static DomainError ForLine(int lineNumber, DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new DomainError(error.Code, $"Line {lineNumber}: {error.Message}");
    }
}

/// <summary>
/// Issues a numbered quote to the session's customer. Every line is priced by the
/// <see cref="PriceResolver"/> (guardrail A1); nothing the caller sends can set a price. A retry with
/// the same request id and lines returns the first quote instead of issuing another (A8).
/// </summary>
/// <remarks>
/// Everything runs in one write transaction: the idempotency check, the next quote number and the
/// save. A failure saves nothing and uses no number, so numbers have no gaps. The idempotency record
/// holds the quote's id; a retry rebuilds the result from the stored quote, whose lines are a
/// snapshot that never changes, so it gets the same figures as the first call.
/// </remarks>
/// <param name="offerings">Resolves each line's offering.</param>
/// <param name="customers">Reads the session's customer.</param>
/// <param name="pricing">Loads the customer's price lists and contracts.</param>
/// <param name="resolver">Works out each line's price.</param>
/// <param name="quotes">Stores the quote.</param>
/// <param name="numbers">Issues the quote number.</param>
/// <param name="idempotency">Remembers request ids.</param>
/// <param name="unitOfWork">Saves it all in one transaction.</param>
/// <param name="time">The clock for the issue date and the pricing moment.</param>
/// <param name="presenter">Prepares the stored quote for showing.</param>
public sealed class CreateQuote(
    OfferingLookup offerings,
    ICustomerReader customers,
    IPricingReader pricing,
    PriceResolver resolver,
    IQuoteRepository quotes,
    IQuoteNumbers numbers,
    IIdempotencyStore idempotency,
    IUnitOfWork unitOfWork,
    TimeProvider time,
    QuotePresenter presenter)
{
    /// <summary>The tool name idempotency records are kept under.</summary>
    public const string ToolName = "create_quote";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    /// <summary>Issues a quote for <paramref name="lines"/>, or returns the one issued for <paramref name="requestId"/>.</summary>
    /// <param name="businessId">The session's business.</param>
    /// <param name="customerId">The session's customer. Never a model argument (guardrail D2).</param>
    /// <param name="agentId">The session's agent, recorded on the quote.</param>
    /// <param name="lines">The offerings and quantities, unchecked.</param>
    /// <param name="requestId">The client's idempotency key, unchecked.</param>
    /// <param name="cancellationToken">Cancels the work. Nothing is saved unless it completes.</param>
    public async Task<Result<IssuedQuote>> ExecuteAsync(
        BusinessId businessId,
        CustomerId customerId,
        AgentId agentId,
        IReadOnlyList<QuoteLineRequest?>? lines,
        string? requestId,
        CancellationToken cancellationToken)
    {
        var key = RequestIdInput.Validate(requestId);
        if (key.IsFailure)
        {
            return Result.Failure<IssuedQuote>(key.Error);
        }

        var checkedLines = CheckLines(lines);
        if (checkedLines.IsFailure)
        {
            return Result.Failure<IssuedQuote>(checkedLines.Error);
        }

        var requested = checkedLines.Value;
        var hash = RequestIdInput.Hash(requested.Select(l => l.Reference.ToUpperInvariant() + '\u001f' + RequestIdInput.Normalize(l.Quantity.Value)));

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var now = time.GetUtcNow();
        var record = await idempotency.FindAsync(key.Value, ToolName, customerId, cancellationToken).ConfigureAwait(false);
        if (record is not null && !record.IsExpired(now))
        {
            return record.Matches(hash)
                ? Result.Success(await ReplayAsync(record, cancellationToken).ConfigureAwait(false))
                : Result.Failure<IssuedQuote>(IdempotencyErrors.Conflict);
        }

        var customer = await customers.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.BusinessId != businessId)
        {
            return Result.Failure<IssuedQuote>(CustomerQueryErrors.CustomerNotFound);
        }

        var priced = await PriceAsync(businessId, customer, requested, now, cancellationToken).ConfigureAwait(false);
        if (priced.IsFailure)
        {
            return Result.Failure<IssuedQuote>(priced.Error);
        }

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, time.LocalTimeZone).DateTime);
        var number = await numbers.NextAsync(today.Year, cancellationToken).ConfigureAwait(false);
        if (number.IsFailure)
        {
            return Result.Failure<IssuedQuote>(number.Error);
        }

        var quote = Quote.Create(QuoteId.New(), number.Value, customer, agentId, today, priced.Value, []);
        if (quote.IsFailure)
        {
            return Result.Failure<IssuedQuote>(quote.Error);
        }

        quotes.Add(quote.Value);
        var response = JsonSerializer.Serialize(new StoredResponse(quote.Value.Id.Value), _json);
        if (record is null)
        {
            idempotency.Add(IdempotencyRecord.Create(key.Value, ToolName, customerId, hash, response, now));
        }
        else
        {
            record.Renew(hash, response, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(await presenter.PresentAsync(quote.Value, customer, cancellationToken).ConfigureAwait(false));
    }

    private static Result<IReadOnlyList<(string Reference, Quantity Quantity)>> CheckLines(IReadOnlyList<QuoteLineRequest?>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return Result.Failure<IReadOnlyList<(string, Quantity)>>(QuoteErrors.Empty);
        }

        if (lines.Count > CreateQuoteErrors.MaxLines)
        {
            return Result.Failure<IReadOnlyList<(string, Quantity)>>(CreateQuoteErrors.TooManyLines);
        }

        var checkedLines = new List<(string, Quantity)>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var reference = lines[i]?.Offering?.Trim() ?? string.Empty;
            if (reference.Length == 0)
            {
                return Result.Failure<IReadOnlyList<(string, Quantity)>>(CreateQuoteErrors.ForLine(i + 1, OfferingLookupErrors.OfferingRequired));
            }

            var quantity = QuantityInput.Validate(lines[i]!.Quantity);
            if (quantity.IsFailure)
            {
                return Result.Failure<IReadOnlyList<(string, Quantity)>>(CreateQuoteErrors.ForLine(i + 1, quantity.Error));
            }

            checkedLines.Add((reference, quantity.Value));
        }

        return Result.Success<IReadOnlyList<(string, Quantity)>>(checkedLines);
    }

    private async Task<Result<IReadOnlyList<PricedOffering>>> PriceAsync(
        BusinessId businessId,
        Customer customer,
        IReadOnlyList<(string Reference, Quantity Quantity)> lines,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var sources = await pricing.GetPriceSourcesAsync(customer, cancellationToken).ConfigureAwait(false);

        // The resolver reads contract validity from the moment's own date, so give it the local day.
        var asOf = TimeZoneInfo.ConvertTime(now, time.LocalTimeZone);
        var priced = new List<PricedOffering>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var found = await offerings.FindAsync(businessId, lines[i].Reference, cancellationToken).ConfigureAwait(false);
            if (found.IsFailure)
            {
                return Result.Failure<IReadOnlyList<PricedOffering>>(CreateQuoteErrors.ForLine(i + 1, found.Error));
            }

            if (sources is null)
            {
                return Result.Failure<IReadOnlyList<PricedOffering>>(CreateQuoteErrors.ForLine(i + 1, PricingErrors.NoPrice));
            }

            var price = resolver.Resolve(new PriceRequest(found.Value, customer, lines[i].Quantity, Percentage.Zero, asOf), sources);
            if (price.IsFailure)
            {
                return Result.Failure<IReadOnlyList<PricedOffering>>(CreateQuoteErrors.ForLine(i + 1, price.Error));
            }

            priced.Add(new PricedOffering(found.Value, price.Value));
        }

        return Result.Success<IReadOnlyList<PricedOffering>>(priced);
    }

    private async Task<IssuedQuote> ReplayAsync(IdempotencyRecord record, CancellationToken cancellationToken)
    {
        var stored = JsonSerializer.Deserialize<StoredResponse>(record.ResponseJson, _json)
            ?? throw new InvalidOperationException("An idempotency record has no response.");
        var quote = await quotes.GetAsync(new QuoteId(stored.QuoteId), cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Idempotency record {record.Key} points to a missing quote.");
        var customer = await customers.GetCustomerAsync(quote.CustomerId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Quote {quote.Number} has no customer.");
        return await presenter.PresentAsync(quote, customer, cancellationToken).ConfigureAwait(false);
    }

    private sealed record StoredResponse(Guid QuoteId);
}
