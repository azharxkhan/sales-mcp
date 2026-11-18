using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>Where a marketing consent decision was captured.</summary>
public enum ConsentSource
{
    /// <summary>A form on the business's website.</summary>
    WebForm = 0,

    /// <summary>A reply by email.</summary>
    Email = 1,

    /// <summary>A phone call, noted by staff.</summary>
    Phone = 2,

    /// <summary>In person, noted by staff.</summary>
    InPerson = 3,

    /// <summary>A signed form or letter.</summary>
    Written = 4,
}

/// <summary>
/// A contact's latest decision about electronic direct marketing (POPIA s69). Marketing needs a
/// recorded opt-in; an opt-out, or no record at all, is not consent.
/// </summary>
/// <remarks>
/// Sales MCP has no outbound marketing tools. This record exists so that a later feature, or the
/// business's own systems, can check consent before contacting anyone.
/// </remarks>
public sealed record MarketingConsent
{
    private MarketingConsent(bool optedIn, DateTimeOffset recordedAt, ConsentSource source)
    {
        OptedIn = optedIn;
        RecordedAt = recordedAt;
        Source = source;
    }

    /// <summary>True for an opt-in, false for an opt-out.</summary>
    public bool OptedIn { get; }

    /// <summary>When the decision was made. Supplied by the caller's clock, never read here.</summary>
    public DateTimeOffset RecordedAt { get; }

    /// <summary>Where it was captured.</summary>
    public ConsentSource Source { get; }

    /// <summary>Records an opt-in.</summary>
    public static Result<MarketingConsent> OptIn(DateTimeOffset recordedAt, ConsentSource source) =>
        Create(optedIn: true, recordedAt, source);

    /// <summary>Records an opt-out.</summary>
    public static Result<MarketingConsent> OptOut(DateTimeOffset recordedAt, ConsentSource source) =>
        Create(optedIn: false, recordedAt, source);

    private static Result<MarketingConsent> Create(bool optedIn, DateTimeOffset recordedAt, ConsentSource source)
    {
        if (recordedAt == default)
        {
            return Result.Failure<MarketingConsent>(CustomerErrors.ConsentTimeRequired);
        }

        return Enum.IsDefined(source)
            ? Result.Success(new MarketingConsent(optedIn, recordedAt, source))
            : Result.Failure<MarketingConsent>(CustomerErrors.OptionUnknown);
    }
}
