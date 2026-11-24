# Tool conventions

Every MCP tool and resource in this project follows these rules. Use this page as the checklist when you review a tool PR. A tool that breaks a rule needs a reason in its PR, and the reason belongs on this page.

**Targets:** MCP spec **2026-07-28** and the official C# SDK **`ModelContextProtocol` 2.4.0** ([ADR-0002](../adr/0002-official-mcp-csharp-sdk.md)). Re-check both at the start of each milestone.

Guardrail ids (A1, D4 and so on) refer to [guardrails.md §4](../research/guardrails.md#4-guardrail-catalogue).

## The rules

| Rule | Detail |
|---|---|
| **Thin adapter** | A tool class validates its input, calls one Application use case and maps the result. Business rules live in Domain and Application, never in the host project ([ADR-0004](../adr/0004-clean-layered-architecture.md)) |
| **Names** | `snake_case`, verb first: `search_catalog`, `get_price`, `create_quote` |
| **Descriptions are written for a model** | One sentence on what the tool does, one on when to use it, one on what it does **not** do. Every parameter is described with its unit and an example. Descriptions are static strings, reviewed in PRs (D5) |
| **The session decides the customer** | Tools read the customer from the session context, never from a model argument (D2, [ADR-0016](../adr/0016-customer-bound-to-session.md)). In self-service there is no customer parameter at all |
| **Validate every input** | Types, ranges and lengths. Ids must exist **and** belong to the session's business and customer (D7). An unknown or out-of-scope id is an error, not an empty result. Another customer's record is "not found", never "forbidden", so ids can't be probed |
| **Structured output** | Return a typed result object, which the SDK serialises as structured content, plus a short text rendering. Field names are stable: they become a contract in M3 |
| **Structured errors** | A business-rule failure is a tool error with the domain error's code and a sentence the model can relay, for example `NoPrice`: "No price is set for ACM-BRG-6204 for this customer. Ask a human for a price." Never a stack trace, never an exception message from a library |
| **Facts carry `asOf`** | Every price, stock and lead-time result includes the time it was true (B1) |
| **Numbers, not pressure** | Results never contain urgency or scarcity language such as "only 3 left!" (B2) |
| **Prices only from the resolver** | No tool accepts a price, discount amount or total from the model. The server prices everything (A1) |
| **Commitments only through documents** | Only `create_quote` (and, from M4, order tools) commits the business, through a numbered document that says it is the offer (A2, [ADR-0015](../adr/0015-commitments-only-through-tool-issued-documents.md)) |
| **Untrusted text is fenced** | Free text from the database (descriptions, notes) goes in a field whose name ends in `Text`, such as `descriptionText`. It is Unicode-normalised, stripped of control and invisible characters, and length-capped. The tool's description says such fields are data, not instructions (D4) |
| **Mutations take `requestId`** | A client-supplied idempotency key. The same key with the same arguments returns the first result. The same key with different arguments fails with `IdempotencyConflict` (A8) |
| **Paging and caps** | Lists take `limit` (default 10, maximum 50) and return `nextCursor`. Payloads are size-capped |
| **Annotations** | Set the SDK's tool annotations on `[McpServerTool]`: `ReadOnly = true` on read-only tools; `Destructive` and `Idempotent` on mutating ones; `OpenWorld = false` everywhere, because tools only touch our own data. Hosts use them to decide when to ask the user |
| **stdout is the protocol** | Nothing in `src` may write to the console. The banned-API list bans `System.Console`, so a stray write fails the build. Log with `ILogger`; in the stdio host every log line goes to stderr |
| **Tests** | Every tool gets tests through the MCP client harness: the happy path, each rule failure by code, input validation, and at least one test that tries to break its guardrail |

## A tool, in outline

```csharp
[McpServerToolType]
public sealed class PricingTools(GetPrice getPrice, ISessionContext session)
{
    [McpServerTool(Name = "get_price", ReadOnly = true, OpenWorld = false)]
    [Description("""
        Returns the price this customer pays for an offering at a given quantity, with VAT and a
        breakdown of how the price was chosen. Use it whenever the buyer asks what something costs.
        Never state a price that did not come from this tool or from a quote.
        """)]
    public async Task<PriceResult> GetPriceAsync(
        [Description("Offering id or SKU, for example ACM-BRG-6204")] string offering,
        [Description("Quantity in the offering's unit of measure, greater than 0, for example 12")] decimal quantity,
        CancellationToken cancellationToken)
    {
        var result = await getPrice.ExecuteAsync(session.CustomerId, offering, quantity, cancellationToken);
        return result.IsSuccess ? PriceResult.From(result.Value) : throw ToolError.From(result.Error);
    }
}
```

`GetPrice`, `PriceResult` and `ToolError` are sketches: the first tool PRs define them. Check every SDK call against the pinned version, because names have changed between SDK releases.

## Resources

Resources follow the same scoping, fencing and stable-field rules as tools. They are read-only and small: a summary the host can attach to the conversation, not a data dump. A resource for another customer is not found.

## Review checklist

- [ ] Name, description and parameter descriptions follow the rules above
- [ ] No business logic in the host project
- [ ] No customer, price or total parameter that the model could abuse
- [ ] Every input validated; out-of-scope ids are "not found"
- [ ] Errors are coded and readable; no stack traces
- [ ] `asOf` on facts; untrusted text fenced
- [ ] Annotations set
- [ ] Harness tests, including a guardrail-breaking attempt
