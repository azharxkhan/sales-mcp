# ADR-0007: ZAR and per-item VAT codes as configurable defaults

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [South Africa market](../research/south-africa-market.md), [ERP order-to-cash §4](../research/erp-order-to-cash.md#4-design-decisions-for-the-sales-mcp-domain)

## Context

Pricing needs a currency and tax rules. The demo businesses are South African, and local detail (ZAR, VAT, POPIA) makes the domain realistic. A single hard-coded rate would be wrong for zero-rated and exempt items, and would break if the rate changed or the project were used elsewhere.

## Decision

We will default to **ZAR** and **South African VAT**, modelled as **per-item VAT codes**:

| Code | Default rate |
|---|---|
| `standard` | 15% |
| `zero-rated` | 0% |
| `exempt` | not charged, shown separately |

Rates live in configuration/data, not code. VAT is calculated **per line** after discounts, and each step appears in the `PriceBreakdown`. Money uses `decimal`, with an explicit currency on every amount, and rounding happens at line level. Locale (`en-ZA`) controls formatting only.

## Consequences

**Positive**
- Realistic local flavour for the demo businesses.
- Correct handling of mixed baskets (standard, zero-rated and exempt lines).
- A rate change or another country is a configuration change.

**Negative / trade-offs**
- Multi-currency (FX) is out of scope for now. Every price list has one currency.
- Tax rules are simplified and **not tax advice**. The docs must say so.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| USD and no tax | Generic and unrealistic, and loses the local differentiation |
| One global VAT rate | Wrong for zero-rated and exempt items |
| External tax engine | Overkill for a demo domain |
