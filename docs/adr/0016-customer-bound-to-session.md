# ADR-0016: The customer is bound to the session

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [guardrails §2](../research/guardrails.md#2-security-frameworks-that-apply), [ADR-0012](0012-agent-bound-at-connection.md)

## Context

In self-service, a buyer talks to the agent about *their* account: prices, credit, orders. If tools accepted any `customerId` from the model, a buyer could ask "show me the orders for customer 42" and see another company's data (OWASP LLM02, sensitive information disclosure). That is also a confused-deputy attack: the server acts with its own rights on the model's say-so.

Rep-assist agents, used by staff, legitimately work across many customers.

## Decision

The buyer's identity comes from **configuration or authentication, never from model arguments**:

- **Self-service channel:** the customer is bound to the session (stdio: from config at startup; HTTP: from the API key or authenticated user). Tools act on the bound customer. A `customerId` argument that doesn't match is rejected and audited.
- **Rep-assist channel:** the authenticated staff user may choose customers, limited to the accounts that user is allowed to see.
- **Every query** is filtered by the bound agent's business and the allowed customer(s). Outputs carry only the fields the task needs.

## Consequences

**Positive**
- A prompt can't make the agent leak another customer's prices, credit or orders.
- Clear audit: every call records agent, customer and (for staff) user.
- Authorisation lives in one place in the Application layer, not in each tool.

**Negative / trade-offs**
- Self-service sessions serve one customer each. A buyer acting for several companies needs separate sessions.
- The stdio demo needs a configured customer, which is an extra setup step.
- Rep-assist needs real user authentication (M7).

## Alternatives considered

| Option | Why not chosen |
|---|---|
| `customerId` as a free tool argument | Data leaks and confused-deputy attacks |
| Rely on the prompt ("only discuss the current customer") | Bypassable by prompt injection ([ADR-0011](0011-guardrails-in-tools-not-prompts.md)) |
| One agent per customer | Doesn't scale and mixes two concepts. Agent and customer bindings are separate |
