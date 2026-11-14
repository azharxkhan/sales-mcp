# ADR-0004: Clean, layered architecture

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §6](../PLAN.md#6-architecture-and-repo-structure), #25

## Context

The business rules (pricing, credit, allocation, discount tiers, playbook rules) are the heart of the project and its guardrails ([ADR-0011](0011-guardrails-in-tools-not-prompts.md)). They must be testable without MCP, a database or an AI model. The MCP SDK and the transport will change over time ([ADR-0002](0002-official-mcp-csharp-sdk.md), [ADR-0006](0006-transports-stdio-then-http.md)), and a second host (HTTP) and an agent runtime come later.

## Decision

We will use a **clean / layered architecture**, with dependencies pointing inward only:

| Layer | Project | Contains | May depend on |
|---|---|---|---|
| Domain | `SalesMcp.Domain` | Entities, value objects, business rules | nothing |
| Application | `SalesMcp.Application` | Use cases, guardrail and playbook enforcement, DTOs, ports | Domain |
| Infrastructure | `SalesMcp.Infrastructure` | EF Core, migrations, seeding, YAML loader | Application, Domain |
| Hosts | `SalesMcp.Server.Stdio`, later `SalesMcp.Server.Http` | MCP SDK wiring, agent binding, DI composition | all of the above |
| Agent runtime (M7) | `SalesMcp.AgentRuntime` | Our own agent loop and chat UI | talks to a host over HTTP only |

Layering is enforced by tests (`LayeringTests`), so a wrong project reference fails CI.

## Consequences

**Positive**
- Rules are unit-tested fast and in isolation, which is how guardrails are proven.
- MCP tools are thin adapters over use cases, so the HTTP host reuses everything.
- SDK churn stays inside the host projects.

**Negative / trade-offs**
- More projects and mapping code than a single-project server.
- Risk of ceremony for simple tools. Keep use cases small and avoid needless abstractions.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Single project, tools calling EF directly | Fastest start, but rules would be untestable without MCP and a database, and tangled with the SDK |
| Vertical slices only | A good fit for many apps, but layers make "rules don't depend on MCP or AI" explicit for the course. Slices can still be used inside Application |
| Microservices | No need at this scale |
