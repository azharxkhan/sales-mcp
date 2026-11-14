# ADR-0014: B2B first

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [ADR-0003](0003-b2b-selling-on-erp-grade-engine.md), [South Africa market](../research/south-africa-market.md), [classic sales guides](../research/classic-sales-guides.md)

## Context

Agents could sell to businesses or to consumers. The research, the selling methods (SPIN, JOLT, Challenger, MEDDPICC) and the maintainer's ERP knowledge are strongest in B2B. Consumer selling brings extra law (e.g. the Consumer Protection Act and its cooling-off rules in South Africa), different payment flows and much higher volume.

## Decision

We will build for **business buyers first**: accounts with contacts, credit limits and terms, VAT numbers, quotes and sales orders. Playbooks and evals assume a B2B buyer. B2C playbooks remain a possible stretch goal after v1.

## Consequences

**Positive**
- Matches the strongest research and domain knowledge.
- Credit terms, quotes and approvals give rich, realistic guardrails to demonstrate.
- Keeps consumer-law scope out of the early milestones.

**Negative / trade-offs**
- Smaller audience than consumer chat commerce.
- Some concepts (accounts, credit) would need adapting for B2C later.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| B2C first | More consumer-law scope, weaker fit with the research and the ERP engine |
| B2B and B2C from day one | Doubles the scope before v1 |
