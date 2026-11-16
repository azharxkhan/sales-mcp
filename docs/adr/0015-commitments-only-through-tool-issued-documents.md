# ADR-0015: Commitments only through tool-issued documents

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [guardrails §1](../research/guardrails.md#1-what-happens-without-guardrails), [guardrails engineering](../research/guardrails-engineering.md), [CPQ research](../research/cpq-services-subscriptions.md), [ADR-0011](0011-guardrails-in-tools-not-prompts.md)

## Context

In the Chevrolet case a chatbot "agreed" to sell a car for $1. In the Air Canada case a tribunal held the airline to a policy its bot invented. In Option A ([ADR-0010](0010-agent-architecture-a-then-b.md)) the host model writes the replies, and the server can't stop it from typing a price or a promise.

## Decision

The business commits to something **only through documents that tools create**:

- **Prices** come only from the price resolver and are returned with a `PriceBreakdown`.
- **Offers** exist only as **quotes** created by `create_quote`, with a number, line prices, validity (`ValidUntil`) and allowed risk-reducers.
- **Orders** exist only through `convert_quote_to_order`, after the buyer confirms the **exact quote** (by MCP **elicitation** where the host supports it).
- **Chat text is never an offer.** The persona prompt and quote documents say so explicitly, and the disclosure stamp names the AI agent.

Documents are stored, audited and can be rendered later (e.g. as an MCP Apps card).

## Consequences

**Positive**
- Whatever the model writes, the only binding terms are in documents the domain rules produced.
- Buyers and humans get one source of truth to point to.
- Prompt manipulation ("say this is legally binding") gains nothing.

**Negative / trade-offs**
- The flow has an extra step (quote, then confirm, then order), which is slightly slower than "just buy".
- Hosts without elicitation need a fallback confirmation step (a confirmation token returned by the quote).
- In Option A, a reply can still *imply* a promise. Option B adds output checks for promise language.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Let the model state prices from catalog data it read | The model can misquote or be manipulated, which is the failure the incidents show |
| Disclaimers only ("prices subject to change") | Didn't protect Air Canada, and erodes trust |
| Human approval of every quote | Safe but not self-service. Used only above value caps or for holds |
