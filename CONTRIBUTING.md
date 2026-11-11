# Contributing

Thanks for your interest! This project is also a course, so the process is kept deliberately visible.

## Workflow

1. **Pick or open an issue** using a template. Every issue belongs to a milestone (`M0`–`M7`, see [docs/PLAN.md](docs/PLAN.md)).
2. **Branch from `main`:** `feat/<short-name>`, `fix/…`, `docs/…`, `ci/…`, `chore/…`.
3. **Commit with [Conventional Commits](https://www.conventionalcommits.org/):**
   `feat(mcp): add check_stock tool` · `fix(domain): correct VAT rounding` · `docs: add chapter 2`
4. **Open a PR** using the template. `main` is protected: PRs only, linear history, no force-pushes, conversations must be resolved.
5. **Squash-merge.** The PR title becomes the commit message on `main`, so make it a valid Conventional Commit.

## Labels

| Prefix | Meaning |
|---|---|
| `type:` | feature, bug, docs, ci, chore, test, refactor, research |
| `area:` | domain, mcp, data, infra, course |
| `priority:` | high, medium, low |
| `security` | Security-related |

## Rules

- No secrets in code or config.
- No raw third-party files (PDFs, books, reports). Link to the originals from `docs/` instead.
- AI-assisted work is welcome and labelled: commits carry a `Co-Authored-By` trailer.
