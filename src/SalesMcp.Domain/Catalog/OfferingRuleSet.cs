using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Catalog;

/// <summary>
/// A business's configuration rules, kept consistent as they are added.
/// </summary>
/// <remarks>
/// At most one rule links an ordered pair of offerings. Adding the same rule again is a duplicate;
/// adding a different kind for the same pair is a conflict (A can't both require and exclude B).
/// Exclusions are symmetric, so "A excludes B" also occupies the pair (B, A). Mutual rules in both
/// directions, such as A requires B and B requires A, are allowed.
/// </remarks>
public sealed class OfferingRuleSet
{
    private readonly List<OfferingRule> _rules = [];

    /// <summary>The rules, in the order they were added.</summary>
    public IReadOnlyList<OfferingRule> Rules => _rules;

    /// <summary>Adds a rule unless it duplicates or conflicts with one already in the set.</summary>
    public Result Add(OfferingRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var existing = _rules.FirstOrDefault(r =>
            r.Links(rule.OfferingId, rule.TargetOfferingId) || rule.Links(r.OfferingId, r.TargetOfferingId));

        if (existing is null)
        {
            _rules.Add(rule);
            return Result.Success();
        }

        return Result.Failure(existing.Kind == rule.Kind ? CatalogErrors.RuleDuplicate : CatalogErrors.RuleConflict);
    }

    /// <summary>Offerings that must be on the same quote as <paramref name="offeringId"/>.</summary>
    public IReadOnlyList<OfferingId> RequiredBy(OfferingId offeringId) =>
        Targets(offeringId, OfferingRuleKind.Requires);

    /// <summary>Offerings suggested alongside <paramref name="offeringId"/>.</summary>
    public IReadOnlyList<OfferingId> RecommendedWith(OfferingId offeringId) =>
        Targets(offeringId, OfferingRuleKind.Recommends);

    /// <summary>Offerings that can't share a quote with <paramref name="offeringId"/>, in either direction.</summary>
    public IReadOnlyList<OfferingId> ExcludedWith(OfferingId offeringId) =>
        [.. _rules
            .Where(r => r.Kind == OfferingRuleKind.Excludes
                && (r.OfferingId == offeringId || r.TargetOfferingId == offeringId))
            .Select(r => r.OfferingId == offeringId ? r.TargetOfferingId : r.OfferingId)];

    private List<OfferingId> Targets(OfferingId offeringId, OfferingRuleKind kind) =>
        [.. _rules.Where(r => r.Kind == kind && r.OfferingId == offeringId).Select(r => r.TargetOfferingId)];
}
