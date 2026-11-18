namespace SalesMcp.Domain.Common;

/// <summary>
/// The id of an agent profile, such as <c>acme-distributor-jolt</c>. Agent profiles are YAML files
/// (M3), so the id is a lower-case slug rather than a Guid. The agent is bound when the connection
/// starts (ADR-0012); no tool takes an agent id from the model.
/// </summary>
public readonly record struct AgentId
{
    /// <summary>Stable error code for an invalid agent id.</summary>
    public const string InvalidCode = "AgentIdInvalid";

    private AgentId(string value) => Value = value;

    /// <summary>The slug.</summary>
    public string Value { get; }

    /// <summary>
    /// Validates an agent id: 2–64 characters of lower-case letters, digits and single hyphens,
    /// starting with a letter and not ending with a hyphen.
    /// </summary>
    public static Result<AgentId> Create(string? input)
    {
        var value = input ?? string.Empty;
        var valid = value.Length is >= 2 and <= 64
            && char.IsAsciiLetterLower(value[0])
            && value[^1] != '-'
            && !value.Contains("--", StringComparison.Ordinal)
            && value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

        return valid
            ? Result.Success(new AgentId(value))
            : Result.Failure<AgentId>(InvalidCode, "An agent id must be a lower-case slug such as acme-distributor-jolt.");
    }

    /// <inheritdoc />
    public override string ToString() => Value;
}
