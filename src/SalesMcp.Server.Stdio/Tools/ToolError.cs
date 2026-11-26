using ModelContextProtocol;
using SalesMcp.Domain.Common;

namespace SalesMcp.Server.Stdio.Tools;

/// <summary>
/// Turns a business-rule failure into a tool error. The SDK returns it to the client as a tool
/// result with <c>isError: true</c> and the text
/// <c>An error occurred invoking '{tool}': {Code}: {Message}</c>, which the model can relay.
/// </summary>
/// <remarks>
/// Only domain errors go through here: their messages are written for buyers and contain no
/// internals. Any other exception is a bug, and the SDK reports it without details.
/// </remarks>
public static class ToolError
{
    /// <summary>The exception to throw from a tool for <paramref name="error"/>.</summary>
    public static McpException From(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new McpException($"{error.Code}: {error.Message}");
    }
}
