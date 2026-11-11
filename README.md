# Sales MCP

An open-source **Model Context Protocol (MCP) server for B2B sales orders**, written in C#/.NET. Through it, AI assistants can check stock, quote, place orders and analyse sales, with ERP-grade business rules and guardrails.

The repo also works as a **step-by-step course** on building production-quality MCP servers and the DevOps around them.

> **Status: planning and research.** No code yet. Start with the plan and the research below.

## Docs

| Document | What's in it |
|---|---|
| [Project plan](docs/PLAN.md) | Vision, decisions, domain model, MCP tools/resources/prompts, architecture, DevOps, milestones |
| [State of sales 2026](docs/research/sales-landscape-2026.md) | Trends, then vs now, how selling evolved, reading list |
| [AI in sales 2023–2026](docs/research/ai-in-sales-2023-2026.md) | What AI was used for, what worked, quarter-by-quarter timeline, buyer complaints |
| [Fixing AI selling](docs/research/fixing-ai-selling.md) | How each complaint could have been fixed, with cases (Klarna, Air Canada, Artisan) |
| [Classic sales guides](docs/research/classic-sales-guides.md) | Sales guides from 1887 to the 2000s and what they teach about AI-era selling |

## Why

- 67% of B2B buyers prefer rep-free buying, and 45% used AI in a recent purchase (Gartner, 2026).
- AI agents deliver the most on structured work such as quotes and order fulfilment (Salesforce, 2026).
- Buyers distrust AI that's generic, pushy, hidden or impossible to escalate. This project is designed around those lessons.

## How this repo is built

Developed with [Claude Code](https://claude.com/claude-code) as a pair programmer. AI-assisted commits carry a `Co-Authored-By` trailer.

## License

MIT (to be added in milestone M0).
