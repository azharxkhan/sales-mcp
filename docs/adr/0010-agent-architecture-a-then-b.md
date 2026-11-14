# ADR-0010: Agent architecture: shape the host's agent first (A), then our own runtime (B)

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §6](../PLAN.md#6-architecture-and-repo-structure), [guardrails §4.1](../PLAN.md#41-guardrails), [agent evaluation](../research/agent-evaluation.md), #16

## Context

The product is "configure an agent, and any MCP-capable AI becomes that agent". There are two ways to get there:

- **Option A:** the MCP server *shapes the host's agent*. The host (Claude Desktop, Claude Code or any client) runs the model. Our server supplies the agent profile (persona, playbook prompt), a scoped set of tools, and guardrails enforced inside those tools.
- **Option B:** we run **our own agent loop** (Claude Agent SDK) that connects to our server over HTTP, with a chat UI.

In A, the host model writes the free-text replies, so the server can guide them but not check them. B gives full control, including output checks on every reply, and is required for the evaluation harness.

## Decision

We will do **A then B**.

- **A (M2–M5):** the server exposes `get_agent_profile`, `agent://current`, the `/sell` prompt and playbook-scoped tools. Every commitment is a tool-issued document ([ADR-0015](0015-commitments-only-through-tool-issued-documents.md)), so the parts that matter don't depend on the reply text.
- **B (M7–M8):** `SalesMcp.AgentRuntime` uses the Claude Agent SDK over Streamable HTTP, adds **output guardrail checks** on every reply, a minimal chat UI, and powers the evaluation harness.

## Consequences

**Positive**
- A teaches pure MCP and works with any client, without hosting a model.
- B adds control, output checks and repeatable evals without rewriting the server: same tools, same guardrails.
- Guardrails live in tools ([ADR-0011](0011-guardrails-in-tools-not-prompts.md)), so they hold in both options.

**Negative / trade-offs**
- In A, reply wording (tone, pressure, implied promises) can only be guided by prompts. This known limit is documented in PLAN §4.1.
- B adds a component, model costs and possibly a second language ([ADR-0001](0001-csharp-dotnet-10.md)).

## Alternatives considered

| Option | Why not chosen |
|---|---|
| A only | No output checks and no controlled environment for evals |
| B only | Loses the "any MCP host" story and delays the first usable version |
| Server-side sampling (server asks the client's model to generate) | Sampling's status in spec 2026-07-28 is uncertain, and it still leaves the client in control of the model |
