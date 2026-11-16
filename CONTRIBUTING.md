# Contributing

Thanks for your interest! This project is also a course, so the process is kept deliberately visible.

## Workflow

1. **Pick or open an issue** using a template. Every issue belongs to a milestone (`M0`–`M10`, see [docs/PLAN.md](docs/PLAN.md)).
2. **Branch from `main`:** `feat/<short-name>`, `fix/…`, `docs/…`, `ci/…`, `chore/…`.
3. **Commit with [Conventional Commits](https://www.conventionalcommits.org/):**
   `feat(mcp): add check_stock tool` · `fix(domain): correct VAT rounding` · `docs: add chapter 2`
4. **Open a PR** using the template. `main` is protected: PRs only, linear history, no force-pushes, conversations must be resolved.
5. **Squash-merge.** The PR title becomes the commit message on `main`, so make it a valid Conventional Commit.

## Code quality

The build enforces `.editorconfig` style rules and .NET analyzers, and **warnings fail the build**. Before opening a PR:

```bash
dotnet format SalesMcp.slnx
dotnet build SalesMcp.slnx -c Release
dotnet test --solution SalesMcp.slnx -c Release
```

Line endings are normalised to LF by `.gitattributes`.

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

## Maintainer setup: Claude PR review

The `Claude PR review` and `@claude` workflows need one repository secret. Without it they **skip with a notice** instead of failing.

```bash
claude setup-token
gh secret set CLAUDE_CODE_OAUTH_TOKEN -R azharxkhan/sales-mcp
```

Only the owner and collaborators can trigger them, and they never run on Dependabot or fork PRs (secrets aren't available there). Never paste the token value anywhere except the secret prompt.
