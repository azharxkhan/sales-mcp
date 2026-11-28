# Chapter 1: What is MCP?

> **Milestone:** M2 · **Spec:** [2026-07-28](https://modelcontextprotocol.io/specification/2026-07-28) · **Next:** [Chapter 2: Hello MCP in C#](02-hello-mcp-in-csharp.md)

## What you'll build

Nothing yet. You'll get the words and the mental model that the next chapters use, and see where each idea lands in this repository.

## Why it matters

A language model can write a convincing answer about a price it has never seen. To sell anything, it has to ask a system that knows. Without a standard, every AI application and every business system need their own custom glue. The **Model Context Protocol (MCP)** is that standard: write one server for your business system, and any MCP-capable application can use it.

That decides where the rules live. The application and the model belong to whoever connects; the server is yours. So every rule that matters goes in the server ([ADR-0011](../adr/0011-guardrails-in-tools-not-prompts.md)).

## Concepts

### Three roles

| Role | What it is | Here |
|---|---|---|
| **Host** | The application the person uses, which runs the model | Claude Desktop, Claude Code |
| **Client** | The part of the host that holds one connection to one server | Inside the host; our tests use the SDK's `McpClient` |
| **Server** | A program that offers data and actions | `SalesMcp.Server.Stdio` |

### Three things a server offers

They differ in **who decides to use them**.

| Feature | Decided by | Methods | Good for | In this repository |
|---|---|---|---|---|
| **Tools** | The model | `tools/list`, `tools/call` | Actions and questions with arguments | `search_catalog`, `get_price`, `create_quote` and more |
| **Resources** | The application | `resources/list`, `resources/templates/list`, `resources/read` | Read-only context to attach | `catalog://current`, `quote://{id}` |
| **Prompts** | The person | `prompts/list`, `prompts/get` | Reusable starting points, often slash commands | None yet: `/sell` and others are planned ([PLAN §5](../PLAN.md#5-mcp-surface)) |

A **tool** has a name, a description, a JSON Schema for its arguments and optionally one for its result. The model reads the name, description and schema, and decides whether to call it. **Annotations** such as `readOnlyHint` and `destructiveHint` tell the host how careful to be. They are hints: a host shouldn't trust them from a server it doesn't trust, and your server must not rely on the host acting on them.

A **resource** is identified by a URI. A fixed one such as `catalog://current` is listed; a **template** such as `offering://{id}` describes a family of them.

### Messages

MCP uses [JSON-RPC 2.0](https://www.jsonrpc.org/specification): requests with an `id`, responses with the same `id`, and notifications with none. In spec 2026-07-28 each request stands on its own: it carries the protocol version and the client's capabilities, and there is no session to set up first. A client can ask what a server supports with `server/discover`, which replaced the older `initialize` handshake. Many tutorials still describe the old handshake, so check which version they target.

A server can fail in two ways:

- A **protocol error** is a JSON-RPC `error`: an unknown tool, or a resource that doesn't exist (`-32602`).
- A **tool execution error** is a normal result with `isError: true`: the call was fine, but the answer is no. The model can read it, explain it or try something else.

### Transports

| Transport | How it works | Use it for |
|---|---|---|
| **stdio** | The client starts the server as a child process and they exchange one JSON message per line on stdin and stdout | A server on the same machine. This project today |
| **Streamable HTTP** | Each message is an HTTP POST to one endpoint; replies are JSON or an event stream | A remote, shared server. Planned for M7 ([ADR-0006](../adr/0006-transports-stdio-then-http.md)) |

The older HTTP+SSE transport is deprecated.

On stdio, **stdout belongs to the protocol**. One stray `Console.WriteLine` corrupts the stream, so logs go to stderr.

### What a server can ask for

With **elicitation**, a server asks the person, through the host, for input or confirmation. We plan to use it so a buyer confirms an exact quote before it becomes an order (M4). Older features that let a server ask the host's model for text (**sampling**) or for its folders (**roots**) are deprecated in 2026-07-28.

## Step by step

Follow one question through the system.

1. The person asks, "What do two of those drills cost?"
2. The host sends the model the conversation and the tools it got from `tools/list`.
3. The model chooses `get_price` and fills in `offering` and `quantity`.
4. The host may ask the person to allow it, then sends `tools/call`.
5. The server checks the arguments, works out the price for **its own** idea of who the customer is, and returns it.
6. The model reads the result and words the answer.

The model never computed a price, and never said who the customer was. Step 5 and the data behind it are yours; the rest isn't.

## What can go wrong

| Mistake | What happens | The fix here |
|---|---|---|
| Putting a rule only in a prompt | Another host, or a persuasive buyer, ignores it | Enforce it in the tool |
| Writing logs to stdout | The client can't parse the stream | Log to stderr; `System.Console` is banned |
| Taking the customer as an argument | One buyer can read another's account | The session decides ([ADR-0016](../adr/0016-customer-bound-to-session.md)) |
| Treating database text as trusted | A product description can give the model orders | Clean it, and name the field `…Text` |
| Returning everything | The model's context fills with noise | Search with pages and limits |

## Check yourself

1. Why is a catalog search a tool, and a catalog summary a resource?
2. A buyer asks for a discontinued item. Protocol error, or tool execution error?
3. Your server marks a tool `readOnlyHint: true`. What does that protect you from?

**Exercise:** list three things an agent selling for your own business would need. For each, decide: tool, resource or prompt, and who should decide to use it.

## Code

- [`src/SalesMcp.Server.Stdio/`](../../src/SalesMcp.Server.Stdio/), the only project that references the MCP SDK
- Reading list: [MCP knowledge base](../research/mcp-knowledge-base.md)
- Next: build the server's first tool in [chapter 2](02-hello-mcp-in-csharp.md)
