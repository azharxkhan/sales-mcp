# CPQ, Services and Subscription Selling

> Research for [#22](https://github.com/azharxkhan/sales-mcp/issues/22). Researched 2026-11-11.
> How Configure-Price-Quote (CPQ) works, how services and subscriptions/usage are priced and sold, and the domain design for the `Service` and `Plan` offerings (M6). It builds on [erp-order-to-cash.md](erp-order-to-cash.md) for products. Items marked *(bg)* come from general background knowledge.

---

## 1. CPQ: Configure → Price → Quote

CPQ software (e.g. Salesforce **Agentforce Revenue Management**, formerly Revenue Cloud CPQ) turns a buyer's needs into an accurate, approved quote.

| Step | What it does | Typical mechanics |
|---|---|---|
| **Configure** | Build a valid bundle | **Rules-based** or **constraint-based** configuration: only compatible options can be chosen, and required items are added automatically (e.g. a server needs a rail kit) |
| **Price** | Calculate the price live as configuration changes | A **price waterfall** *(bg term)*: list price → contract/account adjustments → volume/tier discounts → manual discount → **net unit price**. Salesforce shows this as list price → adjustment matrix → net unit price |
| **Approve** | Route exceptions | Rule-driven approval triggers (discount %, margin, deal size, non-standard terms), with manager → finance sequences and **auto-approve paths** for in-policy quotes |
| **Quote document** | Produce the offer | Branded proposal: products, descriptions, pricing, terms, validity, as PDF/Word |
| *Then* | Contract → order → billing/renewals | Quote-to-cash |

**Mapping to Sales MCP** (most of this is already planned):

| CPQ concept | Sales MCP |
|---|---|
| Configuration rules | **New:** `OfferingRule` (requires / excludes / recommends), enforced by `create_quote` |
| Price waterfall | `PriceBreakdown` from the price resolver (contract → price list → quantity break → list → discount → VAT) |
| Approval triggers | Discount tiers, value caps, credit hold → `request_discount_approval` / `request_human_review` |
| Auto-approve path | Within agent caps = auto (A3) |
| Quote document | Tool-issued quote with number, validity, terms (ADR-0015). Rendered as Markdown/HTML. Later, MCP Apps card |

---

## 2. Selling services

### Pricing models
| Model | How it works | Risk sits with | Good for |
|---|---|---|---|
| **Time & materials (T&M)** | Hours × rate (+ materials, often with a markup) | Buyer | Unclear scope. Most common and simplest |
| **Fixed price** | One price for a defined deliverable | Seller (estimation risk) | Well-defined scope. ~39% of projects (SPI, via vendor) |
| **Not-to-exceed (T&M with a cap)** | T&M up to a ceiling | Shared | Buyer wants a budget certainty |
| **Retainer** | Recurring fee for a block of hours or availability. **Rolling** (unused hours carry over) or limited | Shared | Ongoing support |
| **Value/outcome-based** | Priced on business result | Seller | Mature, measurable outcomes |

### The service sales flow
**Scope first, then price** (the main lesson from the sources):
1. **Discovery:** goals, current state, constraints (SPIN/Gap).
2. **Scope:** deliverables, assumptions, exclusions, milestones.
3. **Estimate:** effort by role × **rate card** *(bg: rates per role/seniority, e.g. junior engineer R650/h, senior R1,100/h)*.
4. **Choose the model:** T&M, fixed, not-to-exceed or retainer.
5. **Proposal / SOW** *(bg)*: scope, deliverables, timeline, acceptance criteria, pricing model, change-request process, terms.
6. **Book resources:** availability of the right people.

### Services domain design (M6)
| Entity / tool | Design |
|---|---|
| `Service` (Offering type) | `PricingModel` (TimeAndMaterials, Fixed, NotToExceed, Retainer), default deliverables, required roles |
| `RateCard` | Per business: role → hourly rate (ZAR), optional per-customer contract rates |
| `estimate_scope(serviceId, answers)` | Returns roles × hours (range), assumptions, exclusions, and a price per model, so the buyer can compare T&M vs fixed |
| `check_availability(serviceId, from, to)` | Resource calendar slots by role |
| `book_service(quoteId, slot)` | Creates a `Booking` (tentative until the quote is accepted) |
| Quote lines for services | Show the model, hours/cap, assumptions and change-request terms. **Risk-reducers:** paid discovery phase, fixed-price phase 1 |
| Guardrails | The agent can't promise scope outside the estimate. Fixed-price quotes above a threshold → human review (estimation risk). No booking without availability |

---

## 3. Selling subscriptions and usage

### Pricing models
| Model | How it bills | Example |
|---|---|---|
| **Flat-rate** | Fixed fee per period | R499/month |
| **Per-unit / per-seat** | Same price per unit | R120/user/month |
| **Tiered (graduated)** | Each band priced separately. Unit price falls as usage rises | First 1,000 invoices R1.00, next 4,000 R0.70… |
| **Volume** | **One** rate for the whole quantity, set by the band reached. Creates "cliffs" at boundaries | 5,000 invoices → all at R0.70 |
| **Usage / metered** | Pay for what you use, measured per period | API calls, invoices processed |
| **Hybrid: base + included + overage** | Base fee includes N units. Extra units charged at an overage rate | R999 incl. 2,000 invoices, then R0.60 each |
| **Commit** | Customer commits to a minimum usage for a lower unit rate | Commit 50k/year at R0.45 |

**Mechanics** *(handled by billing engines such as Stripe Billing, Zuora, Chargebee)*: billing periods, **proration** on mid-period changes, credits, renewals, dunning. Recurly's 2026 data shows the industry shifting from acquisition to **retention** ([sales landscape](sales-landscape-2026.md)), and Salesforce reports usage pricing as the **#1 growth model** ([benchmarks](sales-data-benchmarks.md)).

### Subscriptions domain design (M6)
| Entity / tool | Design |
|---|---|
| `Plan` (Offering type) | `BillingPeriod`, `PricingModel` (Flat, PerUnit, Tiered, Volume, Hybrid), `Tiers[]`, `IncludedUnits`, `OverageRate`, `TrialDays`, `MinimumTerm` |
| `compare_plans(customerId, expectedUsage)` | Calculates the **monthly cost of each plan for the buyer's expected usage** and recommends the cheapest fit (honest, not margin-biased) |
| `estimate_usage_cost(planId, usage)` | Cost breakdown by tier/overage + VAT |
| `start_subscription(quoteId)` | Creates a `Subscription` with start date, trial and term. Requires buyer confirmation (elicitation) |
| Out of scope | Actual billing runs, proration and dunning. Document how this would hand off to Stripe Billing or an ERP |
| Guardrails | **Cancel/downgrade is never obstructed** ([conversational UX](conversational-ux.md)). Trials and auto-renewal terms disclosed up front. Recommend the cheapest plan that fits the stated usage |

---

## 4. Summary: changes to the plan
1. **Add `OfferingRule`** (requires/excludes/recommends) for configuration rules (M1 model, enforced from M3/M4).
2. **Services (M6):** `RateCard`, four pricing models, `estimate_scope` returning a comparison across models, tentative bookings.
3. **Plans (M6):** the full set of pricing models. `compare_plans` uses the buyer's expected usage. Billing execution stays out of scope.
4. **Guardrails:** no scope promises outside estimates, fixed-price risk review, no cancellation obstruction, and up-front disclosure of trial/renewal terms.

---

## Sources
- [Salesforce: what is CPQ](https://www.salesforce.com/sales/cpq/what-is-cpq/?bc=DB)
- [Salesforce: Revenue Cloud / CPQ product page](https://www.salesforce.com/uk/products/sales-cloud/features/cpq-software/)
- [G2: Agentforce Revenue Management (Salesforce CPQ) features](https://g2.com/products/salesforce-cpq/features)
- [Replicon: best pricing models for professional services](https://www.replicon.com/?p=128973)
- [Deltek: consulting pricing models](https://www.deltek.com/resources/articles/consulting-pricing-models/)
- [Rework: consulting engagement models](https://resources.rework.com/libraries/professional-services-growth/consulting-engagement-models)
- [Rework: retainer vs project model](https://resources.rework.com/libraries/professional-services-growth/retainer-vs-project-model)
- [Productive: professional services pricing models 2026](https://productive.io/blog/professional-services-pricing-models)
- [Think Insights: time-and-materials vs fixed price](https://thinkinsights.net/insights/time-and-materials-fixed-price)
- [Stripe: usage-based pricing strategy for SaaS](https://stripe.com/ie/resources/more/usage-based-pricing-strategy-for-saas)
- [Zuora: ultimate guide to usage-based pricing](https://www.zuora.com/guides/ultimate-guide-to-usage-based-pricing/)
- [Chargebee: pricing models](https://www.chargebee.com/billing/pricing-models/)
- [Solvimon: tiered usage-based pricing guide](https://www.solvimon.com/blog/the-guide-to-tiered-usage-based-pricing)
- [Nalpeiron: types of usage-based pricing models](https://docs.nalpeiron.com/education-and-training/licensing-education/usage-billing-basics/types-of-usage-based-pricing-models)
- [Schematic: how to implement usage-based pricing](https://schematichq.com/blog/how-to-implement-usage-based-pricing)
