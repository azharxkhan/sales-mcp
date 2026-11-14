# ADR-0003: B2B selling of products, services and subscriptions on an ERP-grade order engine

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §1, §3–4](../PLAN.md), [ERP order-to-cash](../research/erp-order-to-cash.md), [CPQ for services and subscriptions](../research/cpq-services-subscriptions.md), [sales landscape](../research/sales-landscape-2026.md), #16

## Context

The research found that AI selling fails as generic outbound volume (reply rates ~1–4%, AI SDR churn) and works on **structured, high-intent tasks**: quotes, orders, usage questions. Buyers increasingly prefer rep-free buying (Gartner 2026: 67% of B2B buyers). Usage-based pricing is the main growth model, and products, services and subscriptions sell differently.

A demo domain that is too thin (a toy shop) wouldn't show real business rules. A full ERP would never ship.

## Decision

We will model **B2B selling** of three offering types, **products, services and plans (subscriptions/usage)**, behind one `Offering` model, on top of an **order engine that follows real ERP order-to-cash rules**: quotes with validity, sales orders with statuses, credit hold-and-release, stock allocation and back-orders, a condition-style price resolver with VAT, and an audit trail.

The order engine is the **foundation** (M1–M4), not the whole product. Agents and playbooks ([ADR-0013](0013-agents-and-playbooks-as-data.md)) sit on top. Products ship first (v1.0). Services and plans follow in M6.

## Consequences

**Positive**
- Agents answer from real catalog, price, stock and order data, which is where AI selling works.
- Each rule is a real ERP concept (SYSPRO, SAP, Dynamics patterns), so the code teaches *why*.
- Broad enough to be a platform for many agents and playbooks.

**Negative / trade-offs**
- Large scope. Mitigation: v1.0 is products + two playbooks + guardrails + Docker. Everything else is post-v1 ([PLAN §12](../PLAN.md#12-risks)).
- ERP rules are simplified (e.g. a simple ATP). Docs must say where we deviate from a real ERP.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Outbound prospecting / AI SDR tooling | The research shows it fails and damages trust |
| CRM only (contacts, deals, pipeline) | Many existing MCPs cover it, and there are no commitments or business rules to guardrail |
| Products only | Simpler, but misses the services/subscription shift and type-specific selling |
| Full ERP (purchasing, GL, manufacturing) | Out of scope for a sales agent project |
