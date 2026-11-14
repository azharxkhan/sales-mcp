# ADR-0001: Build on C# and .NET 10 (LTS)

- **Status:** Accepted
- **Date:** 2026-11-14
- **Deciders:** Azhar Khan (maintainer)
- **Related:** [PLAN §2](../PLAN.md#2-key-decisions-each-becomes-an-adr-in-docsadr), #25

## Context

The project is an MCP server with a real business domain (ERP-grade order-to-cash) and doubles as a course and a portfolio piece. We need a language and runtime that:

- the maintainer knows deeply, so the effort goes into the domain, MCP and agents rather than the language;
- has an official MCP SDK (see [ADR-0002](0002-official-mcp-csharp-sdk.md));
- is strong at modelling a typed domain, testing and hosting (generic host, DI, ASP.NET Core);
- will be supported for the life of the project.

Most MCP examples and tutorials are TypeScript or Python. Good C# examples are rare.

## Decision

We will write the server in **C# on .NET 10**, the current LTS release. The SDK version is pinned in `global.json` (with `rollForward: latestFeature`), and shared build settings live in `Directory.Build.props`.

## Consequences

**Positive**
- Strong typing, records and nullable reference types suit the domain model and its rules.
- One stack across server, tests, EF Core, ASP.NET Core (HTTP transport) and tooling.
- LTS support (3 years) covers every planned milestone.
- A well-built C# MCP server is uncommon, so the repo stands out as a reference and course.

**Negative / trade-offs**
- Smaller MCP and agent community in .NET than in TypeScript/Python, so fewer copy-paste examples.
- The Claude Agent SDK (Option B, [ADR-0010](0010-agent-architecture-a-then-b.md)) is TypeScript/Python. The agent runtime may need a second language or the Anthropic .NET SDK. Decide in M7.
- Contributors need the .NET 10 SDK installed.

## Alternatives considered

| Option | Why not chosen |
|---|---|
| TypeScript / Node | Largest MCP ecosystem, but not the maintainer's strongest stack and weaker for a rich typed domain with EF-style persistence |
| Python | Great for AI work, but less suited to a layered, strongly typed business domain and less differentiated |
| .NET 9 (STS) | Shorter support window. No feature we need is missing from 10 |
