using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>How one offering relates to another when building a quote.</summary>
public enum OfferingRuleKind
{
    /// <summary>
    /// The offering can't be sold without the target, for example a server needs a rail kit.
    /// <c>create_quote</c> adds or asks for the target (M4).
    /// </summary>
    Requires = 0,

    /// <summary>
    /// The two offerings can't be on the same quote, for example two incompatible plans.
    /// Symmetric: if A excludes B, B excludes A.
    /// </summary>
    Excludes = 1,

    /// <summary>
    /// The target is a sensible add-on. <c>recommend</c> may suggest it (M3), never as a condition of sale.
    /// </summary>
    Recommends = 2,
}

/// <summary>
/// A configuration rule from one offering to another (CPQ "configure" step). Modelled in M1,
/// enforced by <c>recommend</c> from M3 and <c>create_quote</c> from M4.
/// </summary>
public sealed class OfferingRule
{
    private OfferingRule(OfferingId offeringId, OfferingRuleKind kind, OfferingId targetOfferingId, string reason)
    {
        OfferingId = offeringId;
        Kind = kind;
        TargetOfferingId = targetOfferingId;
        Reason = reason;
    }

    /// <summary>The offering the rule belongs to.</summary>
    public OfferingId OfferingId { get; }

    /// <summary>What the rule says.</summary>
    public OfferingRuleKind Kind { get; }

    /// <summary>The other offering.</summary>
    public OfferingId TargetOfferingId { get; }

    /// <summary>
    /// Why the rule exists, in words an agent can pass on to the buyer. Untrusted catalog text:
    /// fence and sanitise it whenever it is returned to a model (guardrail D4).
    /// </summary>
    public string Reason { get; }

    /// <summary>Validates input and creates a rule. A rule can't point at its own offering.</summary>
    public static Result<OfferingRule> Create(
        OfferingId offeringId,
        OfferingRuleKind kind,
        OfferingId targetOfferingId,
        string? reason)
    {
        if (!Enum.IsDefined(kind))
        {
            return Result.Failure<OfferingRule>(CatalogErrors.OptionUnknown);
        }

        if (offeringId == targetOfferingId)
        {
            return Result.Failure<OfferingRule>(CatalogErrors.RuleTargetsItself);
        }

        var trimmed = reason?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return Result.Failure<OfferingRule>(CatalogErrors.ReasonRequired);
        }

        if (trimmed.Length > CatalogErrors.MaxReasonLength)
        {
            return Result.Failure<OfferingRule>(CatalogErrors.ReasonTooLong);
        }

        return Result.Success(new OfferingRule(offeringId, kind, targetOfferingId, trimmed));
    }

    /// <summary>
    /// True when this rule links <paramref name="from"/> to <paramref name="to"/>. An exclusion links
    /// both ways round.
    /// </summary>
    public bool Links(OfferingId from, OfferingId to) =>
        (OfferingId == from && TargetOfferingId == to)
        || (Kind == OfferingRuleKind.Excludes && OfferingId == to && TargetOfferingId == from);
}
