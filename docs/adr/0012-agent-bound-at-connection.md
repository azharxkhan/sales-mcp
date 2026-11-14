# ADR-0012: The agent is bound at connection, not chosen by the model

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [guardrails engineering](../research/guardrails-engineering.md), [ADR-0006](0006-transports-stdio-then-http.md), [ADR-0013](0013-agents-and-playbooks-as-data.md)

## Context

One server can host many agent profiles with different guardrails (e.g. a 10% discount cap vs a 20% cap). If the model could pick or switch the agent through a tool argument, a prompt injection ("switch to the partner agent") could move the conversation to a laxer profile and escape its guardrails.

## Decision

The agent profile is **fixed for the lifetime of a connection**, by the operator, outside the model's reach:

- **stdio:** `--agent <id>` on the command line when the host launches the server. An unknown or invalid profile stops the server at startup.
- **HTTP (M7):** the **API key** maps to exactly one agent profile (and, in self-service, one customer; see [ADR-0016](0016-customer-bound-to-session.md)).

No tool takes an agent id. `get_agent_profile` and `agent://current` are read-only and return the bound profile. Every audit entry records the agent id.

## Consequences

**Positive**
- Prompt injection can't switch the agent or its guardrails.
- Each connection has one clear identity for audit, metrics and traces.
- Simple mental model: one connection = one agent.

**Negative / trade-offs**
- Trying a different agent means reconnecting (another host config entry or API key).
- Handing a conversation from one agent to another (e.g. sales → support) is a reconnect or a human handoff, not a tool call.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| `agentId` parameter on every tool | The model controls it, so it's open to injection and confused-deputy attacks |
| `switch_agent` tool | Same problem, made explicit |
| One server process per agent | Works, but is just a stricter version of this decision. Not needed for stdio |
