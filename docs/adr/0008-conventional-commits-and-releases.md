# ADR-0008: Conventional Commits and automated semantic-version releases

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §8](../PLAN.md#8-devops-plan), [CONTRIBUTING](../../CONTRIBUTING.md), #5

## Context

The repo is trunk-based with a protected `main` and squash merges, so each PR title becomes one commit on `main`. We want a readable history, a changelog and versioned releases (container images, SBOMs) without manual bookkeeping.

## Decision

We will use **Conventional Commits** for PR titles (and so for squash commits): `feat:`, `fix:`, `docs:`, `ci:`, `chore:`, `refactor:`, `test:`, with optional scopes and `!` for breaking changes. PR titles are linted in CI (#5).

From M5, **release-please** opens release PRs that bump the **semantic version** and update `CHANGELOG.md`. Merging one tags a GitHub Release, which publishes the GHCR image and SBOM. AI-assisted commits carry a `Co-Authored-By` trailer.

## Consequences

**Positive**
- The changelog and version bumps are automatic and consistent.
- History is easy to scan and filter by type.
- Releases are reproducible and tied to a tag.

**Negative / trade-offs**
- Contributors must learn the format. Lint feedback and the PR template help.
- A wrong type (e.g. `chore` for a feature) produces a wrong version bump. Reviewers check titles.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Free-form commits + manual releases | No automatic changelog. Easy to forget steps |
| Manual version tags | Error-prone and needs a separate changelog |
| semantic-release | Similar outcome, but releases straight from `main`. A reviewable release PR suits this repo better |
