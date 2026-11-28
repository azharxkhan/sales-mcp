using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Application.Requests;

/// <summary>Errors from a mutating tool's <c>requestId</c> (guardrail A8).</summary>
public static class IdempotencyErrors
{
    /// <summary>No request id was given.</summary>
    public static readonly DomainError RequestIdRequired = new(
        "RequestIdRequired", "Give a requestId: a new one for each new request, and the same one when you retry.");

    /// <summary>The request id has the wrong shape.</summary>
    public static readonly DomainError RequestIdInvalid = new(
        "RequestIdInvalid",
        $"A requestId is 8 to {IdempotencyRecord.MaxKeyLength} letters, digits, '-', '_', '.' or ':', for example a UUID.");

    /// <summary>The request id was already used for a different request.</summary>
    public static readonly DomainError Conflict = new(
        "IdempotencyConflict",
        "This requestId was already used for a different request. Use a new requestId for a new request.");
}

/// <summary>Checks a request id and fingerprints a request, for every mutating tool.</summary>
public static class RequestIdInput
{
    /// <summary>Shortest request id accepted.</summary>
    public const int MinLength = 8;

    /// <summary>A request id, trimmed, or the reason it isn't one.</summary>
    /// <param name="value">The request id as the tool received it.</param>
    public static Result<string> Validate(string? value)
    {
        var key = value?.Trim() ?? string.Empty;
        if (key.Length == 0)
        {
            return Result.Failure<string>(IdempotencyErrors.RequestIdRequired);
        }

        var valid = key.Length is >= MinLength and <= IdempotencyRecord.MaxKeyLength
            && key.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
        return valid ? Result.Success(key) : Result.Failure<string>(IdempotencyErrors.RequestIdInvalid);
    }

    /// <summary>
    /// SHA-256, as lower-case hex, of the parts in order. Callers normalise the parts first, so a
    /// retry that spells the same request differently (<c>2</c> or <c>2.0</c>) still matches.
    /// </summary>
    public static string Hash(IEnumerable<string> parts)
    {
        ArgumentNullException.ThrowIfNull(parts);
        var text = string.Join('\u001e', parts);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    /// <summary>A quantity written without trailing zeros, so <c>2</c> and <c>2.0</c> hash the same.</summary>
    public static string Normalize(decimal value) =>
        value.ToString("0.############################", CultureInfo.InvariantCulture);
}
