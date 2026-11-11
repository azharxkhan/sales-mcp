# Sales MCP: Project Plan

> **One-liner:** An open-source **MCP server for building B2B sales agents**, written in C#/.NET. You configure an agent (what it sells, *how* it sells, its persona and its guardrails), and any MCP-capable AI becomes that agent. It sells **products, services and subscriptions** using research-backed playbooks, on top of an ERP-grade order engine. The repo also works as a **step-by-step course** on building production-quality MCP servers, AI agents and the DevOps around them.
>
> **Headline:** *How to build sales agents people don't hate:* transparent, consistent, easy to escalate, and guardrailed in code.
>
> Research: [sales landscape](research/sales-landscape-2026.md) · [AI in sales 2023–26](research/ai-in-sales-2023-2026.md) · [fixing AI selling](research/fixing-ai-selling.md) · [book summaries](research/sales-book-summaries.md) · [benchmarks](research/sales-data-benchmarks.md) · [ERP order-to-cash](research/erp-order-to-cash.md) · [SA market](research/south-africa-market.md) · [classic guides](research/classic-sales-guides.md)

---

## 1. Why this project

| Research finding | How the project answers it |
|---|---|
| 67% of B2B buyers prefer rep-free buying, and 45% used AI in a purchase (Gartner 2026) | Agents become a rep-free, AI-native sales channel |
| Mass AI outreach failed (reply rates ~1–4%, AI SDR churn). AI works on **structured, high-intent tasks**: quotes, orders, usage (Salesforce 2026) | Agents serve inbound and existing customers, grounded in real catalog and order data. No outbound blasting |
| 40–60% of lost deals end in **"no decision"** (*JOLT Effect*). Pressure backfires in big deals (*SPIN*) | Selling styles are **playbooks**, so an agent can be decisive and risk-reducing instead of pushy |
| Buyers distrust AI that is generic, hidden, inconsistent or impossible to escalate (Air Canada, Klarna, Artisan) | AI disclosure, one source of truth, human handoff and an audit log are core features |
| Usage pricing is the #1 growth model. Products, services and subscriptions sell differently | One `Offering` model with three types, and type-specific tools |
| POPIA s69/s71, EU AI Act Art. 50 | Consent-aware follow-ups, human review of credit decisions, AI disclosure |

**Portfolio goals:** show (1) MCP and agent design, (2) clean .NET architecture and testing, (3) DevOps: CI/CD, containers, IaC, security scanning, releases, (4) real business-domain modelling (ERP order-to-cash), (5) **AI evaluation**, meaning agent behaviour tested like code.

---

## 2. Key decisions (each becomes an ADR in `docs/adr/`)

| # | Decision | Choice | Why |
|---|---|---|---|
| ADR-0001 | Language / runtime | **C# on .NET 10 (LTS)** | Your strongest stack. C# MCP examples are rare, so the repo stands out |
| ADR-0002 | MCP SDK | Official **`ModelContextProtocol`** NuGet package, **2.x** (+ `ModelContextProtocol.AspNetCore`), targeting spec **2026-07-28**. Pin at M0 ([MCP knowledge base](research/mcp-knowledge-base.md)) | Official SDK, attribute-based tools, generic host |
| ADR-0003 | Domain | **B2B selling** of products, services and subscriptions, on an **ERP-grade order engine** | Broad enough to be a platform. ERP rigour makes it more than a toy |
| ADR-0004 | Architecture | **Clean / layered**: Domain → Application → Infrastructure → MCP host (→ Agent runtime later) | Rules testable without MCP or AI |
| ADR-0005 | Persistence | **EF Core**. SQLite locally, PostgreSQL in Docker/cloud | Zero-setup demo, realistic production database |
| ADR-0006 | Transports | **stdio first**, then **Streamable HTTP** (needed for the agent runtime and hosting) | Simplest to teach, then production |
| ADR-0007 | Locale defaults | **ZAR**, per-item **VAT codes** (15% standard, zero-rated, exempt), configurable | Realistic local flavour ([SA research](research/south-africa-market.md)) |
| ADR-0008 | Commits / releases | **Conventional Commits** + automated semver releases | Readable history, a changelog for free |
| ADR-0009 | Cloud | **AWS** (ECS Fargate or App Runner + RDS) via **Terraform**, GitHub **OIDC** | Builds on your AWS learning. No long-lived keys |
| **ADR-0010** | **Agent architecture** | **A then B.** (A) The MCP server *shapes the host's agent* with an agent profile: playbook prompt, scoped tools, guardrails. (B) Later, our **own agent runtime** (Claude Agent SDK) connects to the server over HTTP | A teaches pure MCP and works with any client. B gives full control, a chat UI and the eval harness |
| **ADR-0011** | **Guardrails live in tools, not prompts** | Every rule that matters (discount caps, credit holds, max options, allowed risk-reducers, AI disclosure) is **enforced server-side**. Prompts only guide style | A model can ignore a prompt. It can't bypass a tool that rejects the call |
| **ADR-0012** | **Agent bound at connection, not chosen by the model** | stdio: `--agent <id>` at startup. HTTP: API key → agent profile. The model can't switch to a laxer agent mid-conversation | Prevents guardrail escape through prompt injection |
| **ADR-0013** | **Playbooks and agents as data** | Versioned YAML files (`agents/*.yaml`, `playbooks/*.yaml`), validated against a JSON Schema at startup and in CI | Reviewable in PRs, testable, no code change to add a selling style |
| **ADR-0014** | **B2B-first** | Business buyers, accounts, credit terms, quotes. B2C playbooks possible later | Strongest research and domain knowledge. Avoids consumer-law scope early |
| **ADR-0015** | **Commitments only through tool-issued documents** | Prices come only from the price resolver. Offers exist only as quotes/orders created by tools. Chat text is never an offer | The Chevrolet "$1 Tahoe" and Air Canada lessons ([guardrails §1](research/guardrails.md#1-what-happens-without-guardrails)) |
| **ADR-0016** | **Customer bound to the session** | In self-service, the buyer's identity comes from config/auth, not from model arguments. Every query is filtered by agent and customer | Prevents data leaks and confused-deputy attacks (OWASP LLM02, MCP guidance) |

---

## 3. Core concepts

### 3.1 Agent profile = five parts

```yaml
# agents/acme-distributor-jolt.yaml
id: acme-distributor-jolt
business: acme-distributors            # which seeded business/catalog
offerings: [product]                   # product | service | plan
playbook: jolt-decisive                # how it sells
persona:
  name: "Thandi (AI sales assistant)"
  tone: friendly-professional
  disclose_ai: true                    # always on; schema forbids false
  locale: en-ZA
channel: self-service                  # self-service | rep-assist | customer-follow-up
guardrails:
  max_discount_percent: 10             # above this → approval flow
  credit_check: hold-and-release       # warn-only | hold-and-release | block
  handoff_when: [deal_value_over: 50000, buyer_asks_for_human, legal_terms_question, credit_hold]
  forbidden: [false_scarcity, false_urgency, invented_social_proof]
  marketing_requires_opt_in: true      # POPIA s69
```

### 3.2 Playbook = how it sells

```yaml
# playbooks/jolt-decisive.yaml
id: jolt-decisive
based_on: "The JOLT Effect (Dixon & McKenna, 2022)"
stages: [qualify, recommend, de-risk, commit]
rules:                                  # enforced in tools (ADR-0011)
  max_options_presented: 2
  must_recommend: true
  allowed_risk_reducers: [trial, phased_delivery, cancel_anytime, return_window]
prompt: prompts/jolt-decisive.md        # style guidance for the model
```

**Planned playbooks** (from [book summaries](research/sales-book-summaries.md)):

| Playbook | Based on | Best for | Milestone |
|---|---|---|---|
| `consultative-spin` | SPIN + Gap Selling + Sandler up-front contract | New customers, complex needs | **M3 (v1)** |
| `jolt-decisive` | JOLT Effect | Indecisive buyers, stalled quotes | **M3 (v1)** |
| `transactional-fast` | Rackham & DeVincentis' transactional segment | Repeat orders, known items | M6 |
| `account-expansion` | Expansion win rates (~45% vs ~20% new) | Upsell and cross-sell to existing customers (no bias in facts) | M6 |
| `challenger-insight` | Challenger Sale | Data-backed insight from purchase history | M8 |

### 3.3 Offerings: one model, three ways to sell

| | **Product** | **Service** | **Plan** (subscription/usage) |
|---|---|---|---|
| Key question | In stock? When can it ship? | Who's available? What's the scope? | Which plan fits their usage? |
| Type-specific tools | `check_stock`, back-orders | `check_availability`, `estimate_scope`, `book_service` | `compare_plans`, `estimate_usage_cost`, `start_subscription` |
| Risk-reducers | Return window, partial delivery | Paid discovery, fixed-price phase 1 | Trial, cancel anytime |
| Demo business (seed) | **Acme Distributors** (SA industrial supplies) | **Nimbus IT Services** | **LedgerLite** (SaaS) |
| Milestone | M1–M4 (v1) | M6 | M6 |

---

## 4. Domain model

```
Business ──< Offering (Product | Service | Plan) ──< PriceListItem / PlanTier / Rate
   │
   ├──< AgentProfile → Playbook        (loaded from YAML, validated)
   │
Customer (Account) ──< Contact (MarketingConsent)
   │  CreditLimit, Balance, OnHold, AllowBackOrders, AllowPartialShipment, VatNumber
   │
   ├──< Quote ──< QuoteLine ──> Offering         (ValidUntil, RiskReducers, PriceBreakdown)
   │      │ convert
   │      ▼
   ├──< SalesOrder ──< OrderLine                  (Draft → Released → PartiallyShipped → Shipped → Invoiced | Cancelled)
   │      ├── OrderHold (reason code: credit | discount-approval | review)   ← order-level
   │      └── BackOrderLine                                                  ← line-level (stock)
   ├──< Booking (Service)                         (M6)
   └──< Subscription (Plan, usage)                (M6)

Conversation/Session (agentId, customerId) ──< AuditEntry (every tool call: who/agent/what/when/result)
HumanReviewRequest (entity, reason, summary, status)
Deal (MEDDPICC fields)                            (M10)
```

**Business rules** (details in [ERP research §4](research/erp-order-to-cash.md#4-design-decisions-for-the-sales-mcp-domain)):
1. **Credit:** hold-and-release by default. Exposure = balance + open orders + this order (quotes excluded). A hold always goes to human review (POPIA s71).
2. **Allocation:** from available stock per warehouse. Shortfall → back-order lines if the customer allows. No allocation while on hold (configurable).
3. **Pricing:** contract price → customer price list → quantity break → list price, then discount, then VAT per line. Returned as a `PriceBreakdown`.
4. **Discounts:** within the agent's cap = auto. Above it = `request_discount_approval`. Hard ceiling = rejected.
5. **Playbook rules:** e.g. `recommend` returns at most N options and must mark one as recommended. `quote` only attaches allowed risk-reducers.
6. **Honesty:** stock, price and lead-time facts come only from data. Agents can't present scarcity or urgency that the data doesn't support.
7. **Disclosure:** every customer-facing output from mutating tools carries "performed by AI agent {name}".
8. **Idempotency:** mutating tools take a `requestId`.
9. **Audit:** every tool call is logged with the agent and customer.

### 4.1 Guardrails

The full catalogue (33 guardrails in six groups, each mapped to an enforcement layer and a milestone) is in **[research/guardrails.md](research/guardrails.md#4-guardrail-catalogue)**. How guardrails are built, the bypass categories we test against, and the fix for each are in **[research/guardrails-engineering.md](research/guardrails-engineering.md)**. The essentials:

| Layer | What it does | Examples |
|---|---|---|
| **L1 Config** | Schema-validated agent/playbook YAML. Unsafe settings are impossible | `disclose_ai` can't be false. Discount caps. Tool allowlist. Value caps |
| **L2 Tool/domain** (hard) | Rules in code that reject or hold the call | Price resolver only. Absolute/cumulative discount tiers. Credit holds. Quote validity. Buyer confirms the exact quote via **MCP elicitation**. Playbook max options |
| **L3 Server policy** | Applied to every call | Agent + customer binding. Rate limits and call caps. Audit log. Disclosure stamp. Kill switch |
| **L4 Prompt** (soft) | Style and scope guidance | Stay on topic. No pressure tactics. Explain handoffs |
| **L5 Verify** | Prove it | Contract snapshot. Adversarial eval personas must cause **zero** guardrail breaks. Guardrail-trigger metrics |

**Known limit:** in Option A the host model writes the free-text replies, so the server can only guide them. That's why every commitment must be a tool-issued document (ADR-0015). Option B (M7) adds **output checks** on every reply.

---

## 5. MCP surface

### Tools
| Tool | Purpose | Milestone |
|---|---|---|
| `get_agent_profile` | Returns the bound agent's persona, playbook stages and guardrails (so the host knows its role) | M3 |
| `search_catalog(query, type?)` | Find offerings | M2 |
| `get_customer(customerId)` / `get_customer_credit` | Account context | M2 |
| `check_stock(sku)` | Availability + `AvailableDate` | M2 |
| `get_price(customerId, offeringId, qty)` | Price with `PriceBreakdown` | M2 |
| `qualify(customerId, answers)` | Records needs (current state → future state) against playbook stages | M3 |
| `recommend(customerId, needs)` | Ranked offerings, capped by playbook rules, one marked recommended | M3 |
| `create_quote(customerId, lines, riskReducers?, requestId)` | Quote with validity and allowed risk-reducers | M2/M3 |
| `convert_quote_to_order(quoteId, requestId)` | Credit check, allocation, back-orders | M4 |
| `get_order_status(orderId)` / `list_backorders` | Fulfilment | M2/M4 |
| `apply_discount` / `request_discount_approval` | Discount guardrail | M4 |
| `request_human_review(entityId, reason)` | Handoff with conversation summary | M4 |
| `check_availability` / `estimate_scope` / `book_service` | Services | M6 |
| `compare_plans` / `estimate_usage_cost` / `start_subscription` | Plans | M6 |
| `score_deal_qualification(dealId)` | MEDDPICC | M10 |

### Resources
`agent://current` · `playbook://{id}` · `catalog://{business}` · `offering://{id}` · `customer://{id}` · `quote://{id}` · `order://{id}` · `policy://discounts` · `policy://returns` · `audit://recent` · `reports://sales-summary/{period}` (M10)

### Prompts
`/sell` (starts a conversation in the bound agent's playbook) · `/needs-assessment {customer}` · `/quote-follow-up {quoteId}` · `/order-follow-up {orderId}` · `/backorder-report` · `/weekly-sales-summary` (M10)

---

## 6. Architecture and repo structure

```
             ┌──────────────────────────┐        ┌──────────────────────────────┐
  Option A   │ MCP host (Claude Desktop │ stdio  │ SalesMcp.Server.Stdio        │
  (M2–M5)    │ / Claude Code / any)     │───────▶│  --agent acme-distributor-jolt│
             └──────────────────────────┘        └──────────────┬───────────────┘
                                                                │
             ┌──────────────────────────┐  HTTP + API key       ▼
  Option B   │ SalesMcp.AgentRuntime    │──────▶ SalesMcp.Server.Http ──▶ Application ──▶ Domain
  (M7–M8)    │ (Claude Agent SDK) + chat│        (key → agent profile)        │
             │ UI + Eval harness        │                                     ▼
             └──────────────────────────┘                              Infrastructure (EF Core, YAML loader)
```

```
sales-mcp/
├── src/
│   ├── SalesMcp.Domain/            # Offerings, customers, quotes, orders, rules (no dependencies)
│   ├── SalesMcp.Application/       # Use cases, guardrail + playbook enforcement, DTOs
│   ├── SalesMcp.Infrastructure/    # EF Core, migrations, seeding, YAML agent/playbook loader
│   ├── SalesMcp.Server.Stdio/      # MCP host (stdio), agent bound by --agent
│   ├── SalesMcp.Server.Http/       # MCP host (Streamable HTTP), API key → agent (M7)
│   └── SalesMcp.AgentRuntime/      # Our own agent loop + chat UI (M7)
├── agents/  · playbooks/  · prompts/   # Agent profiles, playbooks, prompt guidance (YAML/Markdown)
├── schemas/                        # JSON Schemas for agent + playbook files
├── evals/                          # Buyer personas, scenarios, scoring (M8)
├── tests/                          # Domain, Application, Integration (MCP client ↔ server)
├── docs/  (PLAN, adr/, research/, course/)
├── deploy/ (docker/, terraform/)
└── .github/ (workflows, templates)
```

---

## 7. Quality, testing and evaluation

- **Unit:** all business and playbook rules (credit, allocation, pricing, discount tiers, max options, risk-reducers, honesty checks).
- **Integration:** in-process server + SDK MCP client, starting the server bound to each sample agent. Asserts guardrails hold even when the client asks for violations.
- **Schema validation:** every file in `agents/` and `playbooks/` is validated in CI.
- **Contract snapshot:** tool/resource/prompt lists per agent.
- **Evaluation harness (M8):** simulated **buyer personas** (price-sensitive, indecisive, technical evaluator, "just get me a human", adversarial prompt-injector) run scripted scenarios against agent profiles via the agent runtime. Scored on **outcome** (quote/order created), **guardrail breaks** (must be 0), **unnecessary discount**, **correct handoff**, **disclosure present**, **options presented ≤ playbook max**. Runs manually and on a schedule with a capped API budget. Results are published as a CI artifact.

---

## 8. DevOps plan

| Area | What | Milestone |
|---|---|---|
| Branching | Trunk-based, protected `main`, PRs only, squash merge (**done**) | M0 |
| Commits | Conventional Commits, PR-title lint, `Co-Authored-By` for AI-assisted work | M0 |
| CI | format → build → unit → integration → schema validation → coverage | M0–M3 |
| AI review | Claude PR review + `@claude` (**added**, needs the token secret) | M0 |
| Security | CodeQL, Dependabot, vulnerable-package check, secret scanning, Trivy | M0 / M5 |
| Containers | Multi-stage, non-root image. Compose with Postgres | M5 |
| Releases | release-please → GitHub Release + changelog → GHCR image + SBOM | M5 |
| Evals in CI | `evals.yml` (manual + nightly, budget-capped, results artifact) | M8 |
| IaC + CD | Terraform (Fargate/App Runner, RDS, Secrets Manager), OIDC deploys, staging → prod | M9 |
| Observability | Structured logs, OpenTelemetry traces per tool call **tagged with agent id**, health checks | M5–M9 |

---

## 9. Milestones

> M0 issues already exist (#2–#9). Issues for M1+ get opened when each milestone starts.

| Milestone | Release | Scope |
|---|---|---|
| **M0: Foundation** | — | Solution scaffold, quality gates, CI, CodeQL, licence/security docs, ADRs 0001–0016, README diagram |
| **M1: Domain & data** | — | `Offering` model (product implemented, service/plan stubs), customers, price lists, VAT codes, quotes/orders skeleton, **Acme Distributors** seed data (realistic, from [benchmarks](research/sales-data-benchmarks.md)) |
| **M2: First MCP tools** | v0.1.0 | Stdio host, `search_catalog`, `get_customer`, `check_stock`, `get_price`, `create_quote`, `get_order_status`. Claude setup docs + demo GIF. Integration test harness |
| **M3: Agents & playbooks** | v0.2.0 | YAML agent/playbook files + JSON Schema, loader, `--agent` binding, `get_agent_profile`, `agent://`/`playbook://` resources, `/sell` + `/needs-assessment` prompts, `qualify`, `recommend`, **`consultative-spin` and `jolt-decisive`**, playbook rule enforcement |
| **M4: Orders & guardrails** | v0.3.0 | Quote → order (with buyer confirmation), credit hold-and-release, back-orders, discount tiers + approval, per-order/day value caps, `request_human_review`, honesty checks, AI disclosure, rate limits, audit log, agent kill switch |
| **M5: Ship it** | **v1.0.0** | Docker + compose, Postgres + Testcontainers, release automation, GHCR, SBOM, Trivy, OpenTelemetry |
| **M6: Services & subscriptions** | v1.1.0 | Service and Plan offerings + tools, **Nimbus IT** and **LedgerLite** seeds, `transactional-fast` + `account-expansion` playbooks |
| **M7: Agent runtime (Option B)** | v1.2.0 | Streamable HTTP host + API-key → agent and customer binding, Claude Agent SDK runtime, **output guardrail checks on every reply**, minimal web chat UI |
| **M8: Evaluation harness** | v1.3.0 | Buyer personas, scenarios, scoring, `evals.yml`, results report, `challenger-insight` playbook |
| **M9: Cloud deploy** | v1.4.0 | Terraform on AWS, OIDC CD, staging → prod, runbook, cost notes |
| **M10: Analytics & qualification** | v1.5.0 | MEDDPICC `Deal`, `score_deal_qualification`, sales-summary resources, `/weekly-sales-summary` |
| *Stretch* | — | B2C playbooks, agent builder tool (`create_agent`), MCP registry publish, `dotnet tool` install |

**Change from the previous plan:** the ERP order engine is now the foundation (M1–M4) rather than the whole product. Agents/playbooks (M3), services/subscriptions (M6), agent runtime (M7) and evals (M8) are new. AWS moves to M9 and analytics to M10.

---

## 10. Course chapters (`docs/course/`)

1. What is MCP? Tools, resources, prompts, transports
2. Hello MCP in C#: one tool, connected to Claude
3. Modelling a sales domain: offerings, customers, quotes (tests first)
4. Persistence: EF Core, migrations, realistic seed data
5. Real tools: validation, errors, descriptions written for LLMs
6. **Agents as data:** profiles, playbooks, schemas, binding at connection
7. **Guardrails in code:** why prompts aren't enough
8. Orders, credit and back-orders: ERP rules
9. Testing an MCP server end-to-end
10. CI/CD, containers and releases
11. Selling services and subscriptions
12. **Building your own agent runtime** (Claude Agent SDK over HTTP)
13. **Evaluating agents:** simulated buyers and scoring in CI
14. Deploying to AWS with Terraform
15. Analytics and MEDDPICC qualification

---

## 11. Definition of done (every PR)
- [ ] Conventional Commit title, linked issue, PR template filled in
- [ ] CI green: build, format, tests, schema validation, CodeQL
- [ ] New rules/tools/playbooks have unit + integration tests (guardrail violations tested)
- [ ] Docs, tool descriptions and ADRs updated
- [ ] No secrets, no raw third-party files, no local AI tooling files

---

## 12. Risks

| Risk | Mitigation |
|---|---|
| **Scope creep** (now a platform) | v1.0 = products + 2 playbooks + guardrails + Docker. Everything else is post-v1 |
| Prompt injection to bypass guardrails | ADR-0011/0012/0015/0016: enforcement in tools, agent and customer bound at connection, commitments only via documents, adversarial personas in evals ([guardrails](research/guardrails.md)) |
| Eval cost and flakiness | Budget cap, small fixed scenario set, multiple runs per scenario with thresholds, manual/nightly only |
| MCP SDK / Agent SDK API changes | Pin versions, isolate SDK code in host projects, Dependabot + tests |
| Agents perceived as pushy or unethical | Honesty and disclosure rules, no outbound marketing, an ethics metric in evals |
| AWS cost | Smallest tiers, `terraform destroy` documented, billing alarm |

---

## 13. Open questions
1. **Product name:** keep "Sales MCP" or rebrand (e.g. *Sales Agent Kit*)?
2. **Evals budget:** API key with a small monthly cap, or manual-only runs at first?
3. **Chat UI (M7):** minimal Blazor page, or React (your other strong stack)?
