# ADR-0013: Agents and playbooks are data (versioned YAML with a JSON Schema)

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §3](../PLAN.md#3-core-concepts), [book summaries](../research/sales-book-summaries.md), [sales conversation design](../research/sales-conversation-design.md)

## Context

The project's promise is "configure an agent": what it sells, how it sells (a playbook such as consultative SPIN or decisive JOLT), its persona and its guardrails. Adding a selling style or a new agent shouldn't need a code change, and changes to guardrail settings should be reviewed like code. Some settings must never be unsafe (e.g. AI disclosure turned off).

## Decision

Agent profiles and playbooks are **versioned YAML files**:

- `agents/*.yaml`: business, offerings, playbook, persona, channel and guardrails.
- `playbooks/*.yaml`: stages, rules (e.g. `max_options_presented`, `allowed_risk_reducers`) and a link to prompt guidance in `prompts/*.md`.

Each file is validated against a **JSON Schema** in `schemas/` at **startup** (an invalid file stops the server) and in **CI**. The schema makes unsafe settings impossible. For example, `disclose_ai` can only be `true`, and discount caps have hard maximums.

Playbook *rules* are enforced in tools ([ADR-0011](0011-guardrails-in-tools-not-prompts.md)). Only the *prompt* part is style guidance.

## Consequences

**Positive**
- New agents and selling styles are reviewable PRs, with no code change.
- Guardrail settings are visible, diffable and tested.
- Learners can read and write agents without touching C#.

**Negative / trade-offs**
- The schema and the C# model must stay in sync. Mitigation: tests that load every sample file.
- YAML errors are easy to make. Clear validation messages are required.
- Changing a file needs a restart (no hot reload, which is also safer).

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Agents in C# code | Every change needs a developer and a build. Less accessible for the course |
| Agents in the database with an admin UI | More flexible at runtime, but harder to review and version. Possible later (stretch: `create_agent`) |
| Free-form prompt files only | Nothing can be validated or enforced |
| JSON instead of YAML | Valid, but YAML is easier to read and comment for humans. The schema is JSON Schema either way |
