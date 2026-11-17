# Security policy

## Supported versions

Sales MCP hasn't had a release yet. Until v1.0.0, only the latest commit on `main` gets security fixes.

| Version | Supported |
|---|---|
| `main` | ✅ |
| Releases before v1.0.0 | Only the latest |

From v1.0.0 on, the latest minor release of the current major version is supported. This table is updated at each major release.

## Reporting a vulnerability

**Please don't open a public issue, discussion or pull request for a security problem.**

Report it privately through GitHub instead:

1. Open the repository's [**Security** tab](https://github.com/azharxkhan/sales-mcp/security).
2. Choose **Report a vulnerability**.
3. Describe the problem, how to reproduce it, and its impact. A minimal example (an agent file, a sequence of tool calls, or a prompt) helps a lot.

What to expect:

- An acknowledgement within **5 working days**.
- An assessment and, if it's confirmed, a fix plan within **15 working days**. Complex issues may take longer, and you'll be kept updated.
- A private advisory where the fix is worked on, credit in the advisory and release notes unless you'd rather stay anonymous, and a published advisory once a fix is released.

This is a project maintained by one person in their own time, so the timelines are best effort.

## Guardrail bypasses are security issues

Sales MCP enforces its business rules in code, not in prompts ([ADR-0011](docs/adr/0011-guardrails-in-tools-not-prompts.md)). If you find a way to make an agent break one of those rules, please report it as a vulnerability, not as a bug or feature request. Examples:

- getting a discount above the agent's cap, or a price that didn't come from the price resolver;
- placing an order without the buyer's confirmation, or converting an expired or someone else's quote;
- reading another customer's data, or switching to a different agent profile mid-session;
- releasing a credit hold, or getting around a value cap, rate limit or the kill switch;
- making stored text (a product description or customer note) act as instructions to the server.

The full list of guardrails is in [docs/research/guardrails.md](docs/research/guardrails.md#4-guardrail-catalogue).

## Scope

**In scope:** the code in this repository (the MCP server, its tools, resources and prompts, the agent and playbook loader, the agent runtime and chat UI once they exist), the sample agent and playbook files, the container image, the GitHub workflows, and the Terraform in `deploy/`.

**Out of scope:**

- Free-text wording produced by a third-party MCP host's own model. Option A agents can only guide what a host's model writes ([PLAN §4.1](docs/PLAN.md#41-guardrails)). Report it if a *tool* lets the model do something it shouldn't, not because a model said something odd.
- Vulnerabilities in dependencies that are already public and tracked upstream. Dependabot and the daily vulnerability audit handle those, though a report is still welcome if Sales MCP is exploitable through them.
- Problems that need someone to already control the server's configuration or host machine.
- Denial of service through sheer request volume against a deployment you don't own.

Please test only against your own local copy or deployment, never against someone else's, and don't access data that isn't yours.
