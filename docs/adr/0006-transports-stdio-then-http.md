# ADR-0006: stdio first, then Streamable HTTP

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [MCP knowledge base §0](../research/mcp-knowledge-base.md#0-current-state-nov-2026-read-this-first), [ADR-0010](0010-agent-architecture-a-then-b.md), [ADR-0012](0012-agent-bound-at-connection.md)

## Context

MCP defines two transports: **stdio** (the host launches the server as a local process) and **Streamable HTTP** (a remote server). The older HTTP+SSE transport is deprecated. Spec 2026-07-28 makes requests stateless and self-contained, which simplifies HTTP hosting and scaling.

The first milestones target desktop MCP hosts (Claude Desktop, Claude Code). Our own agent runtime, multi-tenant hosting and the cloud deploy need a network transport.

## Decision

We will ship **stdio first** (`SalesMcp.Server.Stdio`, M2) and add **Streamable HTTP** (`SalesMcp.Server.Http`, M7). Both hosts share the same Application layer and tools. We will not implement the deprecated HTTP+SSE transport.

How the agent is bound differs per transport ([ADR-0012](0012-agent-bound-at-connection.md)): `--agent <id>` for stdio, API key → agent profile for HTTP.

## Consequences

**Positive**
- stdio is the simplest way to teach MCP and needs no auth, ports or TLS.
- Works with any desktop MCP host from day one.
- HTTP arrives when it's needed for the agent runtime and AWS hosting.

**Negative / trade-offs**
- stdout is reserved for protocol messages, so all logging in the stdio host must go to stderr.
- Two hosts to test. Integration tests cover both once HTTP exists.
- The HTTP host brings auth, rate limiting and TLS concerns (M7, M9).

## Alternatives considered

| Option | Why not chosen |
|---|---|
| HTTP only | Needs auth and hosting before anything works locally. Harder first chapter |
| stdio only | Can't support our own agent runtime, multi-user hosting or the cloud deploy |
| HTTP+SSE | Deprecated in the spec |
