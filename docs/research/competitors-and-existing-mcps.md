# Competitors and Existing Sales/Commerce MCP Servers

> Research for [#20](https://github.com/azharxkhan/sales-mcp/issues/20). Researched 2026-11-11.
> The market moves monthly. Pricing figures come from secondary sources and **conflict**, so check vendor pricing pages before quoting. Claims found in only one source are flagged.

---

## 1. Landscape at a glance

| Category | Examples | What they are | Relation to us |
|---|---|---|---|
| **CRM-native AI agents** (commercial) | Salesforce **Agentforce**, HubSpot **Breeze**, Microsoft Dynamics 365 Sales agents, Zoho Zia | Agents inside a CRM: SDR/prospecting, inbound qualification, service | Closed and per-platform. We're an open, teachable alternative focused on **selling with ERP rules** |
| **Support / conversation AI** | Intercom **Fin**, Zendesk AI agents, Gladly, Decagon | Resolution-focused service bots, often priced per resolution | Show the **outcome-pricing** trend. Mostly service, not quoting/ordering |
| **AI SDR tools** | 11x, Artisan, Amplemarket | Autonomous outbound | The failure case we design against ([AI in sales](ai-in-sales-2023-2026.md)) |
| **Official CRM MCP servers** | **HubSpot MCP** (GA Apr 2026, free on every tier, OAuth, read/write contacts/deals/tickets), **Salesforce** official server (60+ tools reported) | Give *any* AI data access to the CRM | **Data access, not selling behaviour.** No playbooks, few business guardrails |
| **Commerce MCP servers** | **Shopify Storefront MCP** (GA early 2026; catalog/cart/checkout/orders on UCP), **Stripe MCP** (`mcp.stripe.com`) + ACP, **commercetools Commerce MCP** (preview Feb 2026) + AgenticLift | Let shopping agents browse and buy | **B2C storefront** focused. Weak on B2B account pricing, credit terms, back-orders, quotes |
| **ERP MCPs** | Community **Odoo** servers (including a YAML-driven "security-first" gateway with 27 tools), SAP (mostly roadmap, self-deployed per one critical source) | Expose ERP data and actions | Closest to our engine, but generic CRUD without selling playbooks or evals |
| **Integration hubs** | Metorial (open source), CData, Zapier/Make-style MCP gateways | Many connectors, one endpoint | Breadth, not depth |

---

## 2. Commercial agents in more detail

| | Salesforce Agentforce | HubSpot Breeze |
|---|---|---|
| Positioning | Enterprise "agentic enterprise". Seven role-specific agents shipped Sep 2026 (incl. Piper inbound, Hunter outbound in pilot) | SMB/mid-market. AI included in hubs plus paid agents |
| Pricing signals | Sources conflict: **per task (~$2)** vs **per-user bundles (~$550/user/mo)**. Separate from core licences | **Outcome-based** from Apr 2026: Customer Agent reportedly moved from $1.00/conversation to **$0.50 per resolved conversation** |
| Scale | ~18,500 customers, 3B+ monthly workflows (one source) | Large SMB base |
| Strengths | Data cloud, governance (Agent Fabric, Guardian), orchestration | Ease of setup, free MCP server, pricing |
| Gaps for our use case | Heavy setup and cost. Closed. US-centric | Light on ERP rules (credit holds, back-orders, account pricing) |

*One source claims Salesforce acquired Intercom/Fin in Sep 2026. **Unverified**, so don't quote it.*

**Trend:** pricing is moving from seats to **outcomes** (per resolution or task). That rewards agents that actually resolve, which matches our "measure the buyer's success" principle ([conversational UX](conversational-ux.md)).

---

## 3. Open-source and MCP landscape: what's missing

From directory listings (mcpservers.org, Glama, awesome-mcp lists, vendor round-ups):

1. **CRM MCPs = CRUD.** They expose contacts, deals and tickets. None define *how to sell* (playbooks), and few enforce commercial guardrails.
2. **Commerce MCPs = B2C storefronts.** Cart and checkout for consumers. Little support for **B2B quotes, account/contract pricing, credit limits, back-orders, VAT codes**.
3. **No open "agent profile + playbook" standard.** Agent behaviour lives in closed platforms or prompts.
4. **Evals are rare.** Almost no open servers ship behavioural tests with adversarial buyers.
5. **Very few C#/.NET reference servers** with production DevOps.
6. **Licensing traps:** some community servers are free only for non-commercial use, so check before reusing.

---

## 4. Where Sales MCP fits

| Differentiator | Why it matters | Who doesn't do it |
|---|---|---|
| **Selling styles as data** (agent profiles + playbooks, schema-validated) | Change *how* an agent sells through a reviewable PR | CRM MCPs, commerce MCPs |
| **ERP-grade B2B rules** (credit hold-and-release, back-orders, account pricing, VAT codes) | Real B2B, not just consumer carts | Shopify/Stripe/commercetools MCPs |
| **Guardrails in tools + binding at connection + commitments as documents** | Safe by design ([guardrails](guardrails.md)) | Most community MCPs |
| **Evaluation harness with adversarial buyers** | Agent behaviour tested like code | Almost everyone (open source) |
| **Products, services and subscriptions** in one model | Matches how businesses actually sell | Most tools focus on one |
| **Open source, C#/.NET, full DevOps, a course** | Learnable, forkable, portfolio-grade | Commercial platforms |
| **SA locale** (ZAR, VAT codes, POPIA) | Local relevance | Global tools default to US |

**Interoperability (later):** rather than compete with the CRM and commerce MCPs, the Sales MCP could **sync to them**: push won deals to HubSpot/Salesforce, or read catalogs from Shopify/Odoo. A good stretch goal. Positioning: *"the selling brain with ERP rules, in front of your existing systems."*

---

## Sources
- [Digital Applied: HubSpot vs Salesforce 2026, pricing and AI features](https://www.digitalapplied.com/blog/hubspot-vs-salesforce-2026-pricing-ai-features-comparison)
- [Digital Applied: CRM AI agents guide (Salesforce, HubSpot, Zoho)](https://www.digitalapplied.com/blog/crm-ai-agent-salesforce-hubspot-zoho-2026-guide)
- [Carly: Agentforce, what it is, pricing and alternatives (2026)](https://www.usecarly.com/blog/agentforce/)
- [Vantaige: Breeze vs Agentforce, RevOps cost reality](https://vantaige.io/ar/blog/hubspot-breeze-vs-salesforce-agentforce-2026)
- [Octave: Breeze, Agentforce and Einstein compared](https://www.octavehq.com/post/hubspot-breeze-vs-salesforce-agentforce-vs-einstein)
- [eesel: Agentforce alternatives](https://eesel.ai/blog/salesforce-agentforce-alternatives)
- [Autobound: 12 best AI-powered CRMs](https://www.autobound.ai/blog/top-20-crm-systems-leveraging-ai-for-hyper-personalization-in-2025)
- [Salesforce: Dreamforce 2026 media resources](https://www.salesforce.com/news/dreamforce-26-media-resources/)
- [Fast.io: best MCP servers for CRM](https://www.fast.io/resources/best-mcp-servers-crm/)
- [FullEnrich: best MCP servers for sales teams 2026](https://fullenrich.com/tools/best-mcp-server-for-sales)
- [DEV: CRM MCP servers compared](https://dev.to/grove_chatforest/crm-mcp-servers-salesforce-hubspot-pipedrive-attio-and-beyond-off)
- [mcpservers.org: customer ops MCP servers](https://mcpservers.org/topics/customer-ops-mcp)
- [Glama: customer data platform MCP servers](https://glama.ai/mcp/categories/customer-data-platforms)
- [Metorial: sales integrations (open source)](https://metorial.com/usecases/sales)
- [Inchoo: the state of agentic commerce](https://inchoo.net/ecommerce/agentic-commerce/)
- [commercetools: understanding MCP, ACP and UCP](https://commercetools.com/blog/understanding-mcp-acp-ucp-in-agentic-commerce)
- [commercetools: Stripe Agentic Commerce Suite partnership](https://commercetools.com/press-releases/commercetools-supports-stripes-new-agentic-commerce-suite-giving-enterprise-customers-a-faster-path-to-sell-through-ai-agents-with-transactional-security)
- [Stellagent: commercetools AgenticLift (Jan 2026)](https://stellagent.ai/insights/commercetools-agenticlift-2026-01-22)
- [Stellagent: agentic commerce platforms comparison](https://stellagent.ai/insights/agentic-commerce-platforms-comparison)
- [Elogic: AI agents transacting with B2B e-commerce and ERP](https://elogic.co/?p=469833)
- [Lengow: MCP, the protocol AI shopping agents depend on](https://blog.lengow.com/mcp-the-protocol-every-ai-shopping-agent-depends-on/)
- [Stripe Sessions 2026: agentic commerce for platforms](https://stripe.com/fr-lu/sessions/2026/new-channels-on-the)
