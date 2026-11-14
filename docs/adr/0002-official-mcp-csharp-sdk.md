# ADR-0002: Use the official MCP C# SDK (2.x)

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [MCP knowledge base §0](../research/mcp-knowledge-base.md#0-current-state-nov-2026-read-this-first), [ADR-0006](0006-transports-stdio-then-http.md)

## Context

The server must speak the Model Context Protocol correctly as the spec evolves. In Nov 2026 the latest spec is **2026-07-28** (stateless, self-contained requests; elicitation as the main client feature). The official C# SDK, `ModelContextProtocol` on NuGet, is at **2.x**. Many 2025 tutorials target preview or 1.x APIs that have since changed.

## Decision

We will use the official **`ModelContextProtocol`** package, **2.x**, plus **`ModelContextProtocol.AspNetCore`** for Streamable HTTP. We target spec **2026-07-28**. Versions are pinned in `Directory.Packages.props` (central package management) and updated through Dependabot PRs.

SDK types stay in the host projects (`SalesMcp.Server.*`). Domain and Application never reference the SDK ([ADR-0004](0004-clean-layered-architecture.md)).

## Consequences

**Positive**
- Protocol compliance and spec updates come from the maintainers of the standard.
- Attribute-based tools, resources and prompts plus generic-host integration keep host code small.
- The same SDK provides the test client, so integration tests run an in-process MCP client against the real server.

**Negative / trade-offs**
- The SDK still moves quickly, and major versions may break us. Mitigation: pinned versions, SDK isolated in hosts, integration tests.
- Blog posts and samples often lag the SDK, so our docs must say which version they target.

**Follow-ups**
- Pin the SDK when the first tools land (M2).
- Re-check spec and SDK versions at the start of each milestone. If either major version changes, update or supersede this ADR and ADR-0006 in the same PR.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Hand-rolled JSON-RPC implementation | Teaches the protocol, but is costly to keep compliant and isn't what a production server should do |
| Community C# MCP libraries | Less certain maintenance. The official SDK exists and is the reference |
| Pin to a 1.x / preview SDK | Matches older tutorials but targets superseded spec versions |
