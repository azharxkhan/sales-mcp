# Sales MCP

[![CI](https://github.com/azharxkhan/sales-mcp/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/azharxkhan/sales-mcp/actions/workflows/ci.yml)

An open-source **Model Context Protocol (MCP) server for building B2B sales agents**, written in C#/.NET.

Configure an agent (**what** it sells: products, services or subscriptions; **how** it sells: research-backed playbooks such as consultative SPIN or decisive JOLT; its persona; and its guardrails), and any MCP-capable AI becomes that agent. Under the hood is an ERP-grade order engine: quotes, pricing, credit holds, back-orders.

**How to build sales agents people don't hate:** they always disclose that they're AI, answer from one source of truth, hand off to humans easily, and have their guardrails enforced in code, not just prompts.

The repo also works as a **step-by-step course** on building production-quality MCP servers, AI agents (including evaluating them in CI) and the DevOps around them.

> **Status: M0 Foundation.** The solution skeleton exists, and features start in M2. Start with the plan and the research below.

## Build and test

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download) (pinned in `global.json`).

```bash
dotnet build SalesMcp.slnx
dotnet test --solution SalesMcp.slnx
```

| Project | Role |
|---|---|
| `src/SalesMcp.Domain` | Entities and business rules. No dependencies |
| `src/SalesMcp.Application` | Use cases, guardrail and playbook enforcement. Depends on Domain only |
| `src/SalesMcp.Infrastructure` | EF Core, seeding, YAML agent/playbook loading |
| `src/SalesMcp.Server.Stdio` | MCP host over stdio (M2) |
| `tests/*` | xUnit v3 on Microsoft.Testing.Platform. Layering is enforced by architecture tests |

## Docs

| Document | What's in it |
|---|---|
| [Project plan](docs/PLAN.md) | Vision, decisions (ADRs), agent profiles and playbooks, domain model, MCP surface, architecture, evals, DevOps, milestones |
| [State of sales 2026](docs/research/sales-landscape-2026.md) | Trends, then vs now, how selling evolved, reading list |
| [AI in sales 2023–2026](docs/research/ai-in-sales-2023-2026.md) | What AI was used for, what worked, quarter-by-quarter timeline, buyer complaints |
| [Fixing AI selling](docs/research/fixing-ai-selling.md) | How each complaint could have been fixed, with cases (Klarna, Air Canada, Artisan) |
| [Classic sales guides](docs/research/classic-sales-guides.md) | Sales guides from 1887 to the 2000s and what they teach about AI-era selling |
| [Sales data & benchmarks](docs/research/sales-data-benchmarks.md) | Win rates, cycles, quota attainment, funnel conversion, AI impact, order-to-cash KPIs |
| [Sales book summaries](docs/research/sales-book-summaries.md) | SPIN, Strategic Selling, MEDDPICC, Sandler, Challenger, Gap, JOLT, Voss, Cialdini and more |
| [ERP order-to-cash](docs/research/erp-order-to-cash.md) | How SYSPRO, SAP, Dynamics 365, Odoo, Sage, Business One, NetSuite, Business Central, Acumatica, Epicor, JD Edwards and Infor model quotes, orders, pricing, credit and back-orders |
| [South African market](docs/research/south-africa-market.md) | SA e-commerce and B2B landscape, VAT, POPIA, CPA/ECTA, with design implications |
| [Guardrails](docs/research/guardrails.md) | Incidents (Chevy $1 Tahoe, Air Canada), OWASP LLM Top 10 and MCP risks, agentic-commerce limits, a 33-item guardrail catalogue by enforcement layer |
| [Guardrail engineering](docs/research/guardrails-engineering.md) | How guardrails are built (classifiers, instruction hierarchy, design patterns, elicitation), bypass categories, and the fix for each |
| [MCP knowledge base](docs/research/mcp-knowledge-base.md) | Curated MCP reading list: spec 2026-07-28, C# SDK 2.x, courses, auth, security |
| [Conversational UX](docs/research/conversational-ux.md) | Why AI and sales agents are hard to talk to (loops, gated pricing, deflection, dark patterns) and 10 design principles |
| [Competitors & existing MCPs](docs/research/competitors-and-existing-mcps.md) | Agentforce, Breeze, CRM/commerce/ERP MCP servers, and the gaps Sales MCP fills |
| [Agent evaluation](docs/research/agent-evaluation.md) | τ-bench, pass^k, grader types, and the M8 harness design (personas, metrics, thresholds, cost) |
| [CPQ, services & subscriptions](docs/research/cpq-services-subscriptions.md) | Configure-price-quote, service pricing models and scoping, subscription/usage billing, M6 domain design |
| [Sales conversation design](docs/research/sales-conversation-design.md) | Discovery question bank, LAER objection handling, negotiation, closing and tone for playbook prompts |

## Why

- 67% of B2B buyers prefer rep-free buying, and 45% used AI in a recent purchase (Gartner, 2026).
- AI agents deliver the most on structured work such as quotes and order fulfilment (Salesforce, 2026).
- 40–60% of lost deals end in "no decision" (*The JOLT Effect*), so *how* an agent sells matters as much as what it knows.
- Buyers distrust AI that's generic, pushy, hidden or impossible to escalate. This project is designed around those lessons.

## How this repo is built

Developed with [Claude Code](https://claude.com/claude-code) as a pair programmer. AI-assisted commits carry a `Co-Authored-By` trailer.

## License

MIT (to be added in milestone M0).
