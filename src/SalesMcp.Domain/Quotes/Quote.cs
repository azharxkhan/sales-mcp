using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Quotes;

/// <summary>Where a quote stands. Expiry is worked out from the date, never stored (see <see cref="Quote.IsExpired"/>).</summary>
public enum QuoteStatus
{
    /// <summary>Sent to the buyer and open for acceptance until it expires.</summary>
    Issued = 0,

    /// <summary>The buyer confirmed it and it became an order.</summary>
    Accepted = 1,

    /// <summary>The buyer turned it down (M4).</summary>
    Declined = 2,

    /// <summary>The business withdrew it (M4).</summary>
    Withdrawn = 3,
}

/// <summary>Something offered to lower the buyer's risk instead of a discount, as allowed by the playbook (M3).</summary>
public enum RiskReducer
{
    /// <summary>A trial period.</summary>
    Trial = 0,

    /// <summary>Delivery in phases.</summary>
    PhasedDelivery = 1,

    /// <summary>Cancel at any time.</summary>
    CancelAnytime = 2,

    /// <summary>Returns accepted within the product's return window (only for products that have one, guardrail B3).</summary>
    ReturnWindow = 3,
}

/// <summary>
/// A priced offer to one customer. The only way to make an offer (ADR-0015): chat text is never an
/// offer. Each line keeps the price it was quoted at, and totals are sums of the rounded lines.
/// </summary>
/// <remarks>
/// M1 is a skeleton: creation, totals and expiry. Allowed risk-reducers are checked against the
/// playbook in M3; decline, withdraw and buyer confirmation arrive in M4.
/// </remarks>
public sealed class Quote
{
    private readonly List<QuoteLine> _lines;

#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private Quote()
    {
        _lines = [];
    }
#pragma warning restore CS8618

    private Quote(
        QuoteId id,
        QuoteNumber number,
        CustomerId customerId,
        AgentId agentId,
        DateOnly issuedOn,
        DateOnly validUntil,
        string currency,
        List<QuoteLine> lines,
        List<RiskReducer> riskReducers)
    {
        Id = id;
        Number = number;
        CustomerId = customerId;
        AgentId = agentId;
        IssuedOn = issuedOn;
        ValidUntil = validUntil;
        Currency = currency;
        _lines = lines;
        RiskReducers = riskReducers;
        Status = QuoteStatus.Issued;
    }

    /// <summary>The quote's id.</summary>
    public QuoteId Id { get; }

    /// <summary>The number printed on the quote.</summary>
    public QuoteNumber Number { get; }

    /// <summary>The customer it is addressed to.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>The AI agent that issued it, for the disclosure stamp and the audit trail.</summary>
    public AgentId AgentId { get; }

    /// <summary>Where it stands.</summary>
    public QuoteStatus Status { get; private set; }

    /// <summary>The day it was issued.</summary>
    public DateOnly IssuedOn { get; }

    /// <summary>The last day it can be accepted.</summary>
    public DateOnly ValidUntil { get; }

    /// <summary>The currency of every line.</summary>
    public string Currency { get; }

    /// <summary>The lines, numbered from 1.</summary>
    public IReadOnlyList<QuoteLine> Lines => _lines;

    /// <summary>Risk-reducers offered with the quote, without repeats.</summary>
    public IReadOnlyList<RiskReducer> RiskReducers { get; private set; }

    /// <summary>Net, VAT and total, with VAT shown per code (guardrail E3).</summary>
    public VatSummary Totals => VatSummary.Of(_lines.Select(l => l.Price.Vat), Currency);

    /// <summary>Sum of line discounts.</summary>
    public Money Discount => Money.Sum(_lines.Select(l => l.Price.Discount), Currency);

    /// <summary>Validates the lines and issues a quote valid for <paramref name="validDays"/> days.</summary>
    /// <param name="id">The quote's id.</param>
    /// <param name="number">Its printed number.</param>
    /// <param name="customer">The buyer.</param>
    /// <param name="agentId">The agent issuing it.</param>
    /// <param name="issuedOn">Today, from the caller's clock.</param>
    /// <param name="lines">The offerings and their breakdowns from the price resolver.</param>
    /// <param name="riskReducers">Risk-reducers to offer. Repeats are dropped.</param>
    /// <param name="validDays">Days it stays open after <paramref name="issuedOn"/> (1–90, default 30).</param>
    public static Result<Quote> Create(
        QuoteId id,
        QuoteNumber number,
        Customer customer,
        AgentId agentId,
        DateOnly issuedOn,
        IReadOnlyList<PricedOffering> lines,
        IEnumerable<RiskReducer> riskReducers,
        int validDays = QuoteErrors.DefaultValidDays)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(riskReducers);

        if (validDays is < 1 or > QuoteErrors.MaxValidDays)
        {
            return Result.Failure<Quote>(QuoteErrors.ValidityInvalid);
        }

        var reducers = riskReducers.Distinct().ToList();
        if (!reducers.All(Enum.IsDefined))
        {
            return Result.Failure<Quote>(QuoteErrors.OptionUnknown);
        }

        var check = CheckLines(customer, lines);
        if (check.IsFailure)
        {
            return Result.Failure<Quote>(check.Error);
        }

        var quoteLines = lines.Select((l, i) => new QuoteLine(i + 1, l.Offering, l.Price)).ToList();

        return Result.Success(new Quote(
            id,
            number,
            customer.Id,
            agentId,
            issuedOn,
            issuedOn.AddDays(validDays),
            lines[0].Price.UnitPrice.Currency,
            quoteLines,
            reducers));
    }

    /// <summary>True once <paramref name="today"/> is after <see cref="ValidUntil"/>.</summary>
    public bool IsExpired(DateOnly today) => today > ValidUntil;

    /// <summary>Checks the quote can still be accepted on <paramref name="today"/>.</summary>
    public Result CanBeAccepted(DateOnly today)
    {
        if (Status != QuoteStatus.Issued)
        {
            return Result.Failure(QuoteErrors.NotOpen);
        }

        return IsExpired(today) ? Result.Failure(QuoteErrors.Expired) : Result.Success();
    }

    /// <summary>Records that the buyer turned the quote down. Only an open quote can be declined.</summary>
    public Result Decline()
    {
        if (Status != QuoteStatus.Issued)
        {
            return Result.Failure(QuoteErrors.NotOpen);
        }

        Status = QuoteStatus.Declined;
        return Result.Success();
    }

    /// <summary>Marks the quote accepted. Only called when it becomes an order.</summary>
    internal void MarkAccepted() => Status = QuoteStatus.Accepted;

    private static Result CheckLines(Customer customer, IReadOnlyList<PricedOffering> lines)
    {
        if (lines.Count == 0)
        {
            return Result.Failure(QuoteErrors.Empty);
        }

        var currency = lines[0].Price.UnitPrice.Currency;
        var seen = new HashSet<OfferingId>();

        foreach (var (offering, price) in lines)
        {
            if (offering.BusinessId != customer.BusinessId)
            {
                return Result.Failure(QuoteErrors.BusinessMismatch);
            }

            if (price.OfferingId != offering.Id)
            {
                return Result.Failure(QuoteErrors.LineOfferingMismatch);
            }

            if (price.CustomerId != customer.Id)
            {
                return Result.Failure(QuoteErrors.CustomerMismatch);
            }

            if (price.UnitPrice.Currency != currency)
            {
                return Result.Failure(QuoteErrors.CurrencyMismatch);
            }

            if (!seen.Add(offering.Id))
            {
                return Result.Failure(QuoteErrors.LineDuplicate);
            }
        }

        return Result.Success();
    }
}
