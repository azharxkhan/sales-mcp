# MCP Knowledge Base

> A curated reading list for learning and building with the **Model Context Protocol**, from review feedback on #18: *"find more articles to add for MCP knowledge base."* Checked 2026-11-11. MCP moves fast, so **always prefer the official spec and SDK docs** over blog posts, and check the version a guide targets.

---

## 0. Current state (Nov 2026): read this first

| Item | Status | Impact on this project |
|---|---|---|
| **Spec version** | **2026-07-28** is the latest. The base protocol is now **stateless, self-contained requests with per-request capability negotiation** | Simplifies HTTP hosting and scaling (M7). Confirm our transport design against it |
| **Client features** | **Elicitation** (server asks the user for input/confirmation) is the core client feature. Earlier revisions also had *sampling* and *roots*. A secondary source reports these were deprecated in 2026-07-28, so check the changelog | Supports our choice of **A then B** over a sampling-based design. Elicitation is a great fit for **buyer confirmation** guardrails |
| **Extensions** (opt-in) | **Tasks** (long-running async work), **MCP Apps** (interactive UI inline in chats), **Skills over MCP** | MCP Apps could render quote/confirmation cards later. Tasks suit long-running reports |
| **Transports** | stdio (local) and **Streamable HTTP** (remote). Old HTTP+SSE is deprecated | Matches ADR-0006 |
| **C# SDK** | `ModelContextProtocol` on NuGet, **2.x** (2.4.0 latest). Plus `ModelContextProtocol.AspNetCore` for HTTP | Pin 2.x at M0. Many 2025 tutorials target preview/1.x APIs, so adjust |
| **Governance** | Reported to have moved under the Linux Foundation's Agentic AI Foundation (Dec 2025) *(secondary source)* | Vendor-neutral standard, a good argument for the README |

---

## 1. Official sources (start here)

| Resource | What it's for |
|---|---|
| [modelcontextprotocol.io](https://modelcontextprotocol.io) | Official docs: concepts, quickstarts, guides |
| [Specification (latest)](https://modelcontextprotocol.io/specification/latest) | The authoritative protocol: architecture, base protocol, server and client features, security principles |
| [Docs index for LLMs (`llms.txt`)](https://modelcontextprotocol.io/llms.txt) | Machine-readable index of every docs page |
| [Extensions overview](https://modelcontextprotocol.io/extensions/overview.md) | Tasks, MCP Apps, Skills over MCP |
| [Spec schema (TypeScript)](https://github.com/modelcontextprotocol/specification/blob/main/schema/2026-07-28/schema.ts) | The exact message shapes |
| [C# SDK repo: `modelcontextprotocol/csharp-sdk`](https://github.com/modelcontextprotocol/csharp-sdk) | Our SDK: source, samples, issues |
| [NuGet: ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) | Package versions |
| MCP Inspector *(bg: `npx @modelcontextprotocol/inspector`)* | Debug a server interactively, the standard dev tool |

## 2. .NET / C# guides
| Resource | Notes |
|---|---|
| [Microsoft Learn: build a minimal MCP server in C# (quickstart)](https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/build-mcp-server) | Official. stdio, project templates, publishing to NuGet |
| [.NET Blog: Build an MCP server in C# (Apr 2025)](https://devblogs.microsoft.com/dotnet/build-a-model-context-protocol-mcp-server-in-csharp/) | The SDK launch post. **Preview-era APIs**, so check against 2.x |
| [Dev Leader: building MCP servers and clients in C#, complete guide (2026)](https://www.devleader.ca/2026/07/06/building-mcp-servers-and-clients-in-c-the-complete-guide) | Recent, end-to-end |
| [Code With Mukesh: build an MCP server in C#](https://codewithmukesh.com/blog/build-mcp-server-csharp/) | Practical walkthrough |
| [Fast.io: building MCP servers with .NET](https://fast.io/resources/building-mcp-servers-net/) | DI and hosting patterns |
| [MCP Bundles: build an MCP server in C#](https://www.mcpbundles.com/guides/build-mcp-server-csharp) | Short guide |
| [DEV: simple MCP server and client in C#](https://dev.to/petersaktor/build-a-simple-mcp-server-and-client-in-c-1775) | Client side too, useful for our integration tests |

## 3. Courses (free)
| Course | Notes |
|---|---|
| [Claude Academy: Introduction to Model Context Protocol](https://academy.claude.com/courses/introduction-to-model-context-protocol/introducing-mcp) | Fundamentals: tools, resources, prompts |
| [Claude Academy: MCP Advanced Topics](https://academy.claude.com/courses/model-context-protocol-advanced-topics) | Transports (stdio, HTTP, stateless vs stateful scaling), notifications, progress. Some lessons cover sampling and roots, which may be deprecated in 2026-07-28 |
| [Claude Academy: Building with the Claude API, MCP lesson](https://academy.claude.com/courses/building-with-the-claude-api/introducing-mcp) | MCP in the context of the Claude API |
| [Claude Academy: Claude Code 101, MCP](https://academy.claude.com/courses/claude-code-101/mcp) | Connecting MCP servers to Claude Code (our M2 demo) |

## 4. Architecture and the wider picture
| Resource | Notes |
|---|---|
| [WorkOS: Everything your team needs to know about MCP in 2026](https://workos.com/blog/everything-your-team-needs-to-know-about-mcp-in-2026) | Broad, current overview |
| [Noze: MCP 2026-07-28, stateless protocol and deprecations](https://www.noze.it/en/insights/mcp-2026-07-28-stateless/) | What changed in the latest spec (secondary, so check against the official changelog) |
| [First AI Movers: design the context layer, not just servers](https://articles.firstaimovers.com/articles/mcp-2026-context-layer-for-technical-leaders/) | Design thinking for MCP architectures |
| [AI Engineer: MCP talks](https://www.ai.engineer/topics/model-context-protocol) | Conference talks |

## 5. Auth and remote servers (for M7)
| Resource | Notes |
|---|---|
| [Stack Overflow Blog: authentication and authorization in MCP (Jan 2026)](https://stackoverflow.blog/2026/01/21/is-that-allowed-authentication-and-authorization-in-model-context-protocol/) | Clear explainer of the OAuth-based model |
| [Descope: MCP auth](https://descope.com/blog/post/descope-mcp-auth) | Vendor walkthrough |
| [Which MCP clients a remote server lets in](https://hidekazu-konishi.com/entry/which_mcp_clients_a_remote_mcp_server_lets_in.html) | Client allow-listing considerations |

## 6. Security (see also [guardrails.md](guardrails.md) and [guardrails-engineering.md](guardrails-engineering.md))
| Resource | Notes |
|---|---|
| [OWASP: MCP Security Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/MCP%5FSecurity%5FCheat%5FSheet.html) | **Most authoritative** practical checklist |
| [Checkmarx: MCP security risks and controls](https://checkmarx.com/learn/mcp-security-risks-real-world-incidents-and-security-controls/) | Incidents and controls |
| [Corgea: MCP security best practices](https://corgea.com/learn/mcp-security-best-practices) | Practical list |
| [MCP Servers blog: security best practices 2026](https://blog.mcpservers.org/posts/mcp-security-best-practices) | Tool poisoning, rug pulls, pinning |
| [Design Patterns for Securing LLM Agents against Prompt Injections](https://arxiv.org/pdf/2506.08837) | The research foundation for safe agent design |

## 7. How we'll use this in the course
| Course chapter (PLAN §10) | Primary references |
|---|---|
| 1. What is MCP? | Official docs, spec overview, Claude Academy intro |
| 2. Hello MCP in C# | Microsoft Learn quickstart, C# SDK samples, Inspector |
| 5. Real tools | Spec (tools), SDK attributes, OWASP cheat sheet (input validation) |
| 6–7. Agents as data, guardrails in code | Our guardrails docs, design-patterns paper |
| 9. Testing end-to-end | SDK client APIs, Inspector |
| 12. Own agent runtime (HTTP) | Spec 2026-07-28 base protocol + transports, auth articles, Advanced Topics course |

> **Maintenance:** re-check §0 at the start of each milestone. If the spec or SDK major version changes, update this page and ADR-0002/0006 in the same PR.
