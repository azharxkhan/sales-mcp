# ADR-0009: Deploy to AWS with Terraform and GitHub OIDC

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §8–9](../PLAN.md#8-devops-plan), [ADR-0005](0005-ef-core-sqlite-postgres.md), [ADR-0006](0006-transports-stdio-then-http.md)

## Context

From M7 the server runs over HTTP and needs hosting with a managed Postgres database, secrets and staged deploys. A portfolio goal is to show infrastructure as code and secure CI/CD. Long-lived cloud keys in GitHub secrets are a common security mistake.

## Decision

In M9 we will deploy to **AWS**:

- **Compute:** ECS Fargate or App Runner, running the container image built in M5. Pick one in M9 based on cost and features at that time.
- **Data and secrets:** RDS for PostgreSQL. Secrets Manager for connection strings and API keys.
- **Infrastructure as code:** **Terraform**, under `deploy/terraform/`, with remote state.
- **CD:** GitHub Actions authenticates with **OIDC** to a narrowly scoped IAM role. No long-lived access keys. Staging deploys first, then production after approval.

## Consequences

**Positive**
- Reproducible infrastructure, reviewed in PRs like code.
- No stored cloud credentials.
- Builds on the maintainer's AWS learning and is common in the job market.

**Negative / trade-offs**
- AWS costs money. Mitigation: smallest tiers, a billing alarm, and a documented `terraform destroy`.
- More moving parts than a PaaS. A runbook is required.
- Ties the hosting chapter to one cloud. The container itself stays portable.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| Azure (natural for .NET) | Fine technically, but AWS matches the maintainer's learning path and target roles |
| PaaS (Fly.io, Render, Railway) | Simpler and cheaper, but shows less IaC and cloud skill |
| Kubernetes (EKS) | Too much cost and complexity for one service |
| Long-lived IAM keys in secrets | Security anti-pattern. OIDC removes them |
