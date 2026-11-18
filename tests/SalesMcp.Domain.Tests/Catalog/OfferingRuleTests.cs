using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tests.Catalog;

public class OfferingRuleTests
{
    private static readonly OfferingId _server = OfferingId.New();
    private static readonly OfferingId _railKit = OfferingId.New();

    [Fact]
    public void Creates_a_rule_with_a_trimmed_reason()
    {
        var rule = OfferingRule.Create(_server, OfferingRuleKind.Requires, _railKit, "  Needs rails to mount in a rack. ").Value;

        Assert.Equal(_server, rule.OfferingId);
        Assert.Equal(OfferingRuleKind.Requires, rule.Kind);
        Assert.Equal(_railKit, rule.TargetOfferingId);
        Assert.Equal("Needs rails to mount in a rack.", rule.Reason);
    }

    [Fact]
    public void Rejects_a_rule_that_points_at_itself() =>
        Assert.Equal(
            CatalogErrors.RuleTargetsItself,
            OfferingRule.Create(_server, OfferingRuleKind.Recommends, _server, "Buy two.").Error);

    [Theory]
    [InlineData(null, "ReasonRequired")]
    [InlineData("   ", "ReasonRequired")]
    public void Rejects_a_missing_reason(string? reason, string expectedCode) =>
        Assert.Equal(expectedCode, OfferingRule.Create(_server, OfferingRuleKind.Requires, _railKit, reason).Error.Code);

    [Fact]
    public void Enforces_the_reason_length()
    {
        Assert.True(OfferingRule.Create(_server, OfferingRuleKind.Requires, _railKit, new string('r', 500)).IsSuccess);
        Assert.Equal(
            "ReasonTooLong",
            OfferingRule.Create(_server, OfferingRuleKind.Requires, _railKit, new string('r', 501)).Error.Code);
    }

    [Fact]
    public void Rejects_an_unknown_kind() =>
        Assert.Equal(
            "OptionUnknown",
            OfferingRule.Create(_server, (OfferingRuleKind)9, _railKit, "Reason").Error.Code);

    [Fact]
    public void Exclusions_link_both_ways_round_but_other_kinds_do_not()
    {
        var excludes = OfferingRule.Create(_server, OfferingRuleKind.Excludes, _railKit, "Incompatible.").Value;
        var requires = OfferingRule.Create(_server, OfferingRuleKind.Requires, _railKit, "Needs rails.").Value;

        Assert.True(excludes.Links(_server, _railKit));
        Assert.True(excludes.Links(_railKit, _server));
        Assert.True(requires.Links(_server, _railKit));
        Assert.False(requires.Links(_railKit, _server));
    }
}

public class OfferingRuleSetTests
{
    private readonly OfferingId _a = OfferingId.New();
    private readonly OfferingId _b = OfferingId.New();
    private readonly OfferingId _c = OfferingId.New();

    private static OfferingRule Rule(OfferingId from, OfferingRuleKind kind, OfferingId to) =>
        OfferingRule.Create(from, kind, to, "Reason").Value;

    [Fact]
    public void Starts_empty() => Assert.Empty(new OfferingRuleSet().Rules);

    [Fact]
    public void Adds_rules_in_order()
    {
        var set = new OfferingRuleSet();
        var first = Rule(_a, OfferingRuleKind.Requires, _b);
        var second = Rule(_a, OfferingRuleKind.Recommends, _c);

        Assert.True(set.Add(first).IsSuccess);
        Assert.True(set.Add(second).IsSuccess);
        Assert.Equal([first, second], set.Rules);
    }

    [Theory]
    [InlineData(OfferingRuleKind.Requires)]
    [InlineData(OfferingRuleKind.Excludes)]
    [InlineData(OfferingRuleKind.Recommends)]
    public void Rejects_the_same_rule_twice(OfferingRuleKind kind)
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, kind, _b));

        Assert.Equal(CatalogErrors.RuleDuplicate, set.Add(Rule(_a, kind, _b)).Error);
        Assert.Single(set.Rules);
    }

    [Fact]
    public void A_reversed_exclusion_is_a_duplicate()
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, OfferingRuleKind.Excludes, _b));

        Assert.Equal(CatalogErrors.RuleDuplicate, set.Add(Rule(_b, OfferingRuleKind.Excludes, _a)).Error);
    }

    [Theory]
    [InlineData(OfferingRuleKind.Requires, OfferingRuleKind.Excludes)]
    [InlineData(OfferingRuleKind.Requires, OfferingRuleKind.Recommends)]
    [InlineData(OfferingRuleKind.Excludes, OfferingRuleKind.Recommends)]
    [InlineData(OfferingRuleKind.Recommends, OfferingRuleKind.Requires)]
    public void Rejects_a_different_rule_for_the_same_pair(OfferingRuleKind first, OfferingRuleKind second)
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, first, _b));

        Assert.Equal(CatalogErrors.RuleConflict, set.Add(Rule(_a, second, _b)).Error);
    }

    [Theory]
    [InlineData(OfferingRuleKind.Requires)]
    [InlineData(OfferingRuleKind.Recommends)]
    public void An_exclusion_conflicts_with_any_rule_in_the_other_direction(OfferingRuleKind kind)
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, kind, _b));

        Assert.Equal(CatalogErrors.RuleConflict, set.Add(Rule(_b, OfferingRuleKind.Excludes, _a)).Error);
    }

    [Theory]
    [InlineData(OfferingRuleKind.Requires)]
    [InlineData(OfferingRuleKind.Recommends)]
    public void Allows_mutual_rules_in_both_directions(OfferingRuleKind kind)
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, kind, _b));

        Assert.True(set.Add(Rule(_b, kind, _a)).IsSuccess);
    }

    [Fact]
    public void Answers_what_each_offering_requires_recommends_and_excludes()
    {
        var set = new OfferingRuleSet();
        set.Add(Rule(_a, OfferingRuleKind.Requires, _b));
        set.Add(Rule(_a, OfferingRuleKind.Recommends, _c));
        set.Add(Rule(_b, OfferingRuleKind.Excludes, _c));

        Assert.Equal([_b], set.RequiredBy(_a));
        Assert.Equal([_c], set.RecommendedWith(_a));
        Assert.Empty(set.ExcludedWith(_a));
        Assert.Equal([_c], set.ExcludedWith(_b));
        Assert.Equal([_b], set.ExcludedWith(_c));
        Assert.Empty(set.RequiredBy(_b));
    }

    [Fact]
    public void Rejects_a_null_rule() =>
        Assert.Throws<ArgumentNullException>(() => new OfferingRuleSet().Add(null!));
}
