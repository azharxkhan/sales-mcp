# ADR-0005: EF Core with SQLite locally and PostgreSQL in Docker and cloud

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §2](../PLAN.md#2-key-decisions-each-becomes-an-adr-in-docsadr), [ADR-0009](0009-aws-terraform-oidc.md)

## Context

Learners and reviewers should be able to clone the repo and run the server with zero setup, because MCP hosts launch stdio servers directly. Production hosting needs a real multi-user database. Seed data must be realistic and repeatable.

## Decision

We will use **EF Core** for persistence, with **SQLite** as the default local provider (a file database, created and seeded on first run) and **PostgreSQL** for Docker Compose and the cloud. Configuration selects the provider. Integration tests that need Postgres use **Testcontainers** (M5).

## Consequences

**Positive**
- `dotnet run` works with nothing installed but the SDK, which suits stdio MCP hosts and the course.
- Postgres in Docker and on RDS gives a realistic production setup.
- EF Core is familiar, well documented and testable.

**Negative / trade-offs**
- Provider differences (decimal handling, concurrency tokens, case sensitivity) can hide bugs that only show on Postgres. Mitigation: Postgres integration tests in CI from M5.
- Migrations may need to be kept per provider.
- Money must be stored with explicit precision on both providers.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| In-memory store only | Not realistic, and no migrations or persistence story to teach |
| PostgreSQL everywhere | Needs Docker for every run, which breaks the zero-setup stdio demo |
| Dapper / raw SQL | More control but more code. EF Core suits the course and the domain |
| SQL Server | Heavier locally and costlier on AWS than Postgres |
