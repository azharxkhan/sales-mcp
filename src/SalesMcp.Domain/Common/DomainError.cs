namespace SalesMcp.Domain.Common;

/// <summary>
/// A business-rule failure: something the caller asked for that the rules don't allow,
/// such as "no price is set" or "discount above the agent's cap". Not a bug.
/// </summary>
/// <remarks>
/// The <see cref="Code"/> is stable and machine-readable (tools and tests match on it).
/// The <see cref="Message"/> is a plain sentence a model can relay to a buyer, so it must never
/// contain internal details such as stack traces, margins or other customers' data.
/// </remarks>
public sealed record DomainError
{
    /// <summary>Creates an error.</summary>
    /// <param name="code">Stable PascalCase code, for example <c>NoPrice</c>.</param>
    /// <param name="message">A plain sentence explaining the failure.</param>
    /// <exception cref="ArgumentException">The code or message is empty.</exception>
    public DomainError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Code = code;
        Message = message;
    }

    /// <summary>Stable, machine-readable code.</summary>
    public string Code { get; }

    /// <summary>Plain-language explanation, safe to show a buyer.</summary>
    public string Message { get; }

    /// <inheritdoc />
    public override string ToString() => $"{Code}: {Message}";
}
