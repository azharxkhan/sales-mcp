using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Requests;

/// <summary>
/// The first result of a mutating tool call, kept under the client's <c>requestId</c> so a retry gets
/// that result instead of doing the work twice (guardrail A8). Unique per key, tool and customer, so
/// one customer's key can never reach another's result.
/// </summary>
public sealed class IdempotencyRecord
{
    /// <summary>How long a record answers retries. After that the key can be used again.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    /// <summary>Longest key accepted.</summary>
    public const int MaxKeyLength = 100;

    /// <summary>Longest tool name stored.</summary>
    public const int MaxToolLength = 64;

    /// <summary>Length of a request hash: SHA-256 as lower-case hex.</summary>
    public const int HashLength = 64;

#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private IdempotencyRecord()
    {
    }
#pragma warning restore CS8618

    private IdempotencyRecord(string key, string tool, CustomerId customerId, string requestHash, string responseJson, DateTimeOffset createdAt)
    {
        Key = key;
        Tool = tool;
        CustomerId = customerId;
        RequestHash = requestHash;
        ResponseJson = responseJson;
        CreatedAt = createdAt;
    }

    /// <summary>The client's request id.</summary>
    public string Key { get; }

    /// <summary>The tool it was sent to, for example <c>create_quote</c>.</summary>
    public string Tool { get; }

    /// <summary>The session's customer.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>A hash of the arguments, to tell a retry from a different request under the same key.</summary>
    public string RequestHash { get; private set; }

    /// <summary>What the first call produced, as JSON.</summary>
    public string ResponseJson { get; private set; }

    /// <summary>When the first call succeeded.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Records the first result of a call.</summary>
    public static IdempotencyRecord Create(string key, string tool, CustomerId customerId, string requestHash, string responseJson, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(tool);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseJson);
        return new IdempotencyRecord(key, tool, customerId, requestHash, responseJson, createdAt);
    }

    /// <summary>True once the record is older than <see cref="Lifetime"/> at <paramref name="now"/>.</summary>
    public bool IsExpired(DateTimeOffset now) => now - CreatedAt >= Lifetime;

    /// <summary>True when <paramref name="requestHash"/> is the request this record answered.</summary>
    public bool Matches(string requestHash) => string.Equals(RequestHash, requestHash, StringComparison.Ordinal);

    /// <summary>Reuses an expired record's key for a new request.</summary>
    /// <exception cref="InvalidOperationException">The record has not expired.</exception>
    public void Renew(string requestHash, string responseJson, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(responseJson);
        if (!IsExpired(createdAt))
        {
            throw new InvalidOperationException("Only an expired idempotency record can be reused.");
        }

        RequestHash = requestHash;
        ResponseJson = responseJson;
        CreatedAt = createdAt;
    }
}
