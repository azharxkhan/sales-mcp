# Sales MCP: Project Plan

> **One-liner:** An open-source **Model Context Protocol server for B2B sales orders**, written in C#/.NET. AI assistants can check stock, quote, place orders and analyse sales through it, with ERP-grade business rules and guardrails. The repo also works as a **step-by-step course** on building production-quality MCP servers and the DevOps around them.
>
> Background research: [sales-landscape-2026.md](research/sales-landscape-2026.md) · [ai-in-sales-2023-2026.md](research/ai-in-sales-2023-2026.md)

---

## 1. Why this project

| Research finding | How the project answers it |
|---|---|
| 67% of B2B buyers prefer rep-free buying, and 45% used AI in a purchase (Gartner 2026) | The MCP **is** a rep-free, AI-native sales channel |
| AI agents deliver most where tasks are structured: **quotes, order fulfilment, usage tracking** (Salesforce 2026) | The core tools are exactly those |
| Most AI deployments fail on **data quality** and missing governance | One source of truth, validated inputs, business rules, audit log |
| Buyers distrust hidden selling and bots that can't escalate | No sales bias in tool output, transparent AI actions, a `request_human_review` tool |
| Agentic commerce (ACP / UCP / AP2) is arriving | The design shows the "seller-side system an agent talks to" pattern |

**Portfolio goals:** show (1) MCP and API design, (2) clean .NET architecture and testing, (3) DevOps: CI/CD, containers, IaC, security scanning, releases, (4) the ability to model a real business domain (ERP sales orders).

---

## 2. Key decisions (each becomes an ADR in `docs/adr/`)

| # | Decision | Choice | Why |
|---|---|---|---|
| ADR-0001 | Language / runtime | **C# on .NET 10 (LTS)** | Your strongest stack. Far fewer C# MCP examples exist than TS/Python ones, so the repo stands out |
| ADR-0002 | MCP SDK | Official **`ModelContextProtocol`** NuGet package (+ `ModelContextProtocol.AspNetCore` for HTTP). Pin the latest stable at M0 | Official SDK, attribute-based tools, built on generic host |
| ADR-0003 | Domain | **ERP sales orders**: customers, stock, price lists, quotes, orders | Differentiated, and rich in business rules |
| ADR-0004 | Architecture | **Clean / layered**: Domain → Application → Infrastructure → MCP host | Business rules are testable without MCP. Easy to teach |
| ADR-0005 | Persistence | **EF Core**. SQLite for local/stdio, PostgreSQL in Docker/cloud | Zero-setup demo, plus a realistic production database |
| ADR-0006 | Transports | **stdio first** (Claude Desktop/Code), **Streamable HTTP** later | stdio is the simplest to teach. HTTP is needed for hosting |
| ADR-0007 | Locale defaults | Currency **ZAR**, **VAT 15%**, both configurable | Realistic, and gives the project a distinctive local flavour |
| ADR-0008 | Commits / releases | **Conventional Commits** + automated semver releases | A changelog for free, and a readable history |
| ADR-0009 | Cloud | **AWS ECS Fargate + RDS (or App Runner)** via **Terraform**, GitHub **OIDC** | Builds on your AWS/Terraform learning. No long-lived keys |

---

## 3. Domain model (v1)

```
Customer ──< Quote ──< QuoteLine >── StockItem
   │           │ (convert)              │
   │           ▼                        ├── Warehouse stock (OnHand, Allocated, OnOrder)
   └──────< SalesOrder ──< OrderLine >──┘
               │
               ├── Status: Draft → Released → PartiallyShipped → Shipped → Invoiced | Cancelled
               └── BackOrderLine (when stock is short)

PriceList ──< PriceListItem (StockItem, UnitPrice, MinQty)
Customer → PriceList, CreditLimit, Balance, OnHold, SalesRep
Deal (M7): MEDDPICC fields, Stage, Value, linked Quote
AuditEntry: who/what/when for every mutating tool call
```

**Business rules (the "real logic" that makes it more than CRUD):**
1. **Credit check:** an order is blocked if `Balance + OrderTotal > CreditLimit` or the customer is on hold.
2. **Stock allocation:** allocate from available stock (`OnHand − Allocated`). Any shortfall becomes back-order lines.
3. **Pricing:** customer price list → quantity breaks → default list price. VAT is applied per line.
4. **Discount guardrail:** ≤ 10% applies automatically. 10–20% needs an approval token from `request_discount_approval`. > 20% is rejected.
5. **Quote expiry:** quotes expire after 30 days (configurable). Expired quotes can't be converted.
6. **Idempotency:** mutating tools accept a client `requestId`, so a retrying agent can't create duplicate orders.
7. **Audit:** every write produces an `AuditEntry`. Exposed read-only as a resource.

---

## 4. MCP surface

### Tools (actions)
| Tool | Milestone | Mutates? |
|---|---|---|
| `search_customers(query)` | M2 | no |
| `get_customer_credit(customerId)` | M2 | no |
| `search_stock(query)` / `check_stock(sku, warehouse?)` | M2 | no |
| `get_price(customerId, sku, qty)` | M2 | no |
| `create_quote(customerId, lines[], requestId)` | M2 | yes |
| `convert_quote_to_order(quoteId, requestId)` | M4 | yes |
| `get_order_status(orderId)` | M2 | no |
| `list_backorders(customerId?)` | M4 | no |
| `apply_discount(quoteId, percent, approvalToken?)` | M4 | yes |
| `request_discount_approval(quoteId, percent, reason)` | M4 | yes |
| `cancel_order(orderId, reason)` | M4 | yes |
| `request_human_review(entityId, reason)` | M4 | yes |
| `score_deal_qualification(dealId)` (MEDDPICC) | M7 | no |

### Resources (read-only context)
`customer://{id}` · `order://{id}` · `quote://{id}` · `pricelist://{id}` · `stock://{sku}` · `policy://discounts` · `audit://recent` · `reports://sales-summary/{period}` (M7)

### Prompts (reusable templates)
`/quote-for-customer {customer}` · `/order-follow-up {orderId}` · `/backorder-report` · `/weekly-sales-summary` (M7) · `/needs-assessment {customer}` (fit questions, SPIN-style)

**Tool design rules:** clear `[Description]` text, typed inputs validated with domain errors, errors returned as tool errors (not exceptions), small structured outputs, and no upsell or sponsored bias in results.

---

## 5. Architecture and repo structure

```
sales-mcp/
├── src/
│   ├── SalesMcp.Domain/          # Entities, value objects, business rules (no dependencies)
│   ├── SalesMcp.Application/     # Use cases/services, interfaces, DTOs, validation
│   ├── SalesMcp.Infrastructure/  # EF Core DbContext, migrations, repositories, seeding
│   ├── SalesMcp.Server.Stdio/    # MCP host: stdio transport (console app)
│   └── SalesMcp.Server.Http/     # MCP host: Streamable HTTP (ASP.NET Core) + auth + health
├── tests/
│   ├── SalesMcp.Domain.Tests/        # Pure unit tests for rules
│   ├── SalesMcp.Application.Tests/
│   └── SalesMcp.IntegrationTests/    # Real MCP client ↔ server, Testcontainers Postgres
├── docs/
│   ├── PLAN.md  · adr/  · research/
│   └── course/                   # Teaching chapters (see §8)
├── deploy/
│   ├── docker/                   # Dockerfile(s), compose.yaml
│   └── terraform/                # AWS infra (M6)
├── .github/
│   ├── workflows/                # ci.yml, codeql.yml, release.yml, deploy.yml
│   ├── ISSUE_TEMPLATE/ · pull_request_template.md · dependabot.yml · CODEOWNERS
├── Directory.Build.props · Directory.Packages.props   # Central package management, analyzers, nullable
├── .editorconfig · global.json · SalesMcp.sln
├── README.md · CONTRIBUTING.md · SECURITY.md · LICENSE (MIT) · CHANGELOG.md
```

---

## 6. Quality and testing

- **Unit:** every business rule (credit, allocation, pricing, discount tiers, expiry, idempotency). Target ≥ 90% coverage on Domain.
- **Integration:** start the server in-process, connect with the SDK's MCP client, then call `tools/list` and each tool. Assert on schemas and results. Postgres via **Testcontainers**.
- **Contract snapshot:** snapshot the tool/resource/prompt list so accidental breaking changes fail CI.
- **Static checks:** `dotnet format --verify-no-changes`, warnings-as-errors, nullable enabled, .NET analyzers.
- **Manual:** the MCP Inspector, plus a documented Claude Desktop / Claude Code config.

---

## 7. DevOps plan

| Area | What | Milestone |
|---|---|---|
| Branching | Trunk-based: `main` protected, short-lived `feat/*` `fix/*` branches, PRs required, squash merge | M0 |
| Commits | Conventional Commits (`feat:`, `fix:`, `docs:`, `ci:`…). Claude co-authored commits are allowed and labelled | M0 |
| CI (`ci.yml`) | restore → format check → build → unit tests → integration tests → coverage report on PR | M0–M1 |
| Security | CodeQL, Dependabot (NuGet + Actions + Docker), `dotnet list package --vulnerable`, secret scanning, Trivy image scan | M0 / M5 |
| Containers | Multi-stage Dockerfile (SDK → `aspnet` chiseled/non-root runtime), `compose.yaml` with Postgres, health checks | M5 |
| Releases (`release.yml`) | release-please (or semantic-release) → tag → GitHub Release + changelog → image to **GHCR** with SBOM + provenance | M5 |
| IaC | Terraform: VPC, ECS Fargate service, RDS Postgres, ALB/HTTPS, Secrets Manager, CloudWatch. Remote state in S3. `plan` posted on PRs | M6 |
| CD (`deploy.yml`) | GitHub OIDC → AWS role. Deploy on release to `staging`, manual approval to `prod` (GitHub Environments) | M6 |
| Observability | Structured logging (Serilog/ILogger JSON), OpenTelemetry traces per tool call, `/health` endpoints | M5–M6 |
| Repo hygiene | Issue/PR templates, labels, milestones, CODEOWNERS, badges in README | M0 |

**Security for the HTTP transport (M6):** API-key or OAuth bearer auth, per-client rate limiting, input size limits, least-privilege DB user, secrets never in config files. Tool-level authorisation: read-only clients can't call mutating tools.

---

## 8. Course chapters (`docs/course/`)

Each chapter matches a milestone and a git tag (`ch01`, `ch02`…), so learners can check out the state at any point.

1. **What is MCP?** Tools vs resources vs prompts, transports, the request lifecycle
2. **Hello MCP in C#:** a minimal stdio server with one tool, connected to Claude
3. **Modelling the domain:** entities, rules and tests first
4. **Persistence:** EF Core, SQLite, migrations, seed data
5. **Real tools:** validation, error handling, good descriptions for LLMs
6. **Resources and prompts**
7. **Guardrails:** approvals, idempotency, audit, human escalation
8. **Testing an MCP server end-to-end**
9. **CI/CD with GitHub Actions**
10. **Containerising and releasing**
11. **Going remote:** HTTP transport, auth, deploying to AWS with Terraform
12. **Analytics and MEDDPICC:** sales insights over MCP

---

## 9. Milestones and issues

> Each milestone is a GitHub Milestone. Each bullet is roughly one issue → one PR.

### M0: Foundation (repo + CI skeleton)
- `git init`, .NET 10 solution, project layout, `global.json`, `Directory.Build.props`, central package management
- `.editorconfig`, analyzers, nullable, warnings-as-errors
- README (pitch, architecture diagram, roadmap), LICENSE, CONTRIBUTING, SECURITY, CODE_OF_CONDUCT
- `ci.yml` (build + test + format), CodeQL, Dependabot
- Issue/PR templates, labels, branch protection guide
- ADR-0001…0005

### M1: Domain and data
- Domain entities and value objects (Money, Sku, Quantity)
- Business rules with unit tests (credit, allocation, pricing, VAT)
- EF Core DbContext, SQLite, migrations
- Deterministic seed data (~50 customers, ~200 SKUs, 3 warehouses, price lists, history)

### M2: First MCP tools over stdio → **v0.1.0**
- Stdio host with the generic host + DI
- Read tools: `search_customers`, `get_customer_credit`, `search_stock`, `check_stock`, `get_price`, `get_order_status`
- `create_quote` with idempotency
- Claude Desktop / Claude Code setup docs and a demo GIF
- Integration test harness (MCP client ↔ server)

### M3: Resources and prompts → **v0.2.0**
- Resource templates (`customer://`, `order://`, `stock://`, `policy://`)
- Prompts: `/quote-for-customer`, `/order-follow-up`, `/backorder-report`, `/needs-assessment`
- Tool/resource contract snapshot test

### M4: Orders and guardrails → **v0.3.0**
- `convert_quote_to_order`, allocation and back-orders, `list_backorders`, `cancel_order`
- Discount tiers + `request_discount_approval` + `apply_discount`
- `request_human_review`, audit log + `audit://recent`
- Quote expiry

### M5: Ship it (containers + releases) → **v1.0.0**
- Multi-stage Dockerfile, non-root, health check. `compose.yaml` with Postgres
- PostgreSQL provider and Testcontainers integration tests
- release-please, GHCR publish, SBOM, Trivy scan
- Structured logging and OpenTelemetry

### M6: Remote MCP on AWS → **v1.1.0**
- Streamable HTTP host, auth, rate limiting, read-only vs read-write scopes
- Terraform modules, remote state, `terraform plan` on PRs
- OIDC deploy workflow, staging → prod with approvals
- Runbook + cost notes (and how to tear it all down)

### M7: Sales analytics and MEDDPICC → **v1.2.0**
- Deal entity with MEDDPICC fields, `score_deal_qualification`
- `reports://sales-summary/{period}`: revenue, top customers, win rate, quote→order conversion, back-order ageing
- `/weekly-sales-summary` prompt

### Stretch
- Subscription / recurring order lines and a churn report
- A sample agent client (Claude Agent SDK) that runs an end-to-end "quote to order" flow
- Publish to an MCP registry. `dotnet tool` packaging for one-command install

---

## 10. Definition of done (every PR)
- [ ] Conventional Commit title, linked issue, PR template filled in
- [ ] CI green: build, format, tests, CodeQL
- [ ] New rules/tools have unit and integration tests
- [ ] Tool descriptions and docs updated. ADR added if a decision changed
- [ ] No secrets, no copyrighted third-party files (see `docs/research/reports/.gitignore`)

---

## 11. Risks

| Risk | Mitigation |
|---|---|
| MCP SDK API changes | Pin versions, isolate SDK usage in the host projects, Dependabot PRs gated by tests |
| Scope creep | Ship v1.0 at M5. Everything after it is optional |
| AWS cost | Fargate Spot/smallest RDS or App Runner, `terraform destroy` documented, billing alarm |
| Commit history unclear about AI involvement | `Co-Authored-By` trailers plus a "How this repo was built" section in the README |

---

## 12. Open questions for you
1. **Project name:** keep "Sales MCP", or something brandable (e.g. *OrderDesk MCP*, *Quotebook*)?
2. **Course depth:** full chapters as written, or shorter "notes per milestone"?
3. **Cloud:** AWS ECS Fargate (more DevOps to show) or App Runner (cheaper, simpler)?
4. **Public from day one?** Recommended: yes, so the commit history is visible as it grows.
