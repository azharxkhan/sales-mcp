# Architecture Decision Records

Short records of the decisions that shape Sales MCP: the context, what we chose, what it costs, and what we turned down. They expand the table in [PLAN §2](../PLAN.md#2-key-decisions-each-becomes-an-adr-in-docsadr).

## Index

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-csharp-dotnet-10.md) | Build on C# and .NET 10 (LTS) | Accepted |
| [0002](0002-official-mcp-csharp-sdk.md) | Use the official MCP C# SDK (2.x) | Accepted |
| [0003](0003-b2b-selling-on-erp-grade-engine.md) | B2B selling of products, services and subscriptions on an ERP-grade order engine | Accepted |
| [0004](0004-clean-layered-architecture.md) | Clean, layered architecture | Accepted |
| [0005](0005-ef-core-sqlite-postgres.md) | EF Core with SQLite locally and PostgreSQL in Docker and cloud | Accepted |
| [0006](0006-transports-stdio-then-http.md) | stdio first, then Streamable HTTP | Accepted |
| [0007](0007-locale-zar-and-vat-codes.md) | ZAR and per-item VAT codes as configurable defaults | Accepted |
| [0008](0008-conventional-commits-and-releases.md) | Conventional Commits and automated semantic-version releases | Accepted |
| [0009](0009-aws-terraform-oidc.md) | Deploy to AWS with Terraform and GitHub OIDC | Accepted |
| [0010](0010-agent-architecture-a-then-b.md) | Agent architecture: shape the host's agent first (A), then our own runtime (B) | Accepted |
| [0011](0011-guardrails-in-tools-not-prompts.md) | Guardrails live in tools, not prompts | Accepted |
| [0012](0012-agent-bound-at-connection.md) | The agent is bound at connection, not chosen by the model | Accepted |
| [0013](0013-agents-and-playbooks-as-data.md) | Agents and playbooks are data (versioned YAML with a JSON Schema) | Accepted |
| [0014](0014-b2b-first.md) | B2B first | Accepted |
| [0015](0015-commitments-only-through-tool-issued-documents.md) | Commitments only through tool-issued documents | Accepted |
| [0016](0016-customer-bound-to-session.md) | The customer is bound to the session | Accepted |

**Guardrail core:** 0011, 0012, 0015 and 0016 together are the design-based defences against prompt injection and data leaks. See [guardrails engineering](../research/guardrails-engineering.md).

## Writing a new ADR

1. Copy [0000-template.md](0000-template.md) to `NNNN-kebab-case-title.md` with the next free number.
2. Fill in context, decision, consequences and alternatives. Keep it short.
3. Add a row to the index above and open a PR with a `docs(adr):` title.
4. Accepted ADRs aren't edited for substance. To change a decision, write a new ADR that supersedes the old one, and set the old one's status to `Superseded by ADR-XXXX`.
