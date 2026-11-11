# Agent Evaluation Methods (Design Input for the M8 Harness)

> Research for [#21](https://github.com/azharxkhan/sales-mcp/issues/21). Researched 2026-11-11.
> How AI agents are evaluated today, and a concrete design for evaluating Sales MCP agents. Later benchmark versions marked *(secondary)* weren't verified against primary sources.

---

## 1. The reference benchmark: τ-bench (tau-bench)

**τ-bench** (Sierra, June 2024, arXiv 2406.12045) is the closest match to what we're building: an agent **talks to a simulated user**, **calls domain APIs** and must **follow written policies**.

| Aspect | How τ-bench does it | Lesson for us |
|---|---|---|
| Domains | **Retail** and **airline** customer service with policy documents (refund rules, change fees) | Our domain is similar: retail-like B2B selling with policies (discounts, credit) |
| Simulated user | An LLM plays the customer with a hidden goal and persona | Buyer personas with hidden goals (e.g. "buy 500 bolts, max R2,000, needs delivery Friday") |
| **Scoring by end state** | Compares the **database state** at the end with an annotated **goal state** (not the wording of the chat) | Score on *what happened*: quote/order created correctly, no illegal discount, handoff created when required |
| **pass^k** | Probability the agent succeeds on **all k** independent runs of the same task. Reliability fell sharply with k (as low as ~25% at pass^8 on retail for the tested model) | Production needs **consistency**, so report pass^k, not just average success |
| Later versions *(secondary)* | **τ²-bench** (2025) adds telecom and "dual control" (the user also has tools). Later variants for banking/voice are reported | Future: buyer-side agents that also act |

**pass@k vs pass^k:** pass@k = at least one of k tries worked (good for "can it ever?"). pass^k = all k tries worked (good for "can we trust it?"). **Guardrail tests must use pass^k with k ≥ 5:** one break in five runs is a fail.

---

## 2. General best practice (Anthropic, "Demystifying evals for AI agents", and practitioners)

| Practice | Meaning | Applied here |
|---|---|---|
| **Three grader types** | **Code-based** (deterministic checks), **model-based** (LLM-as-judge with a rubric), **human** (spot checks, calibration) | Code for state and guardrails. LLM judge for tone/UX. Human review of a sample |
| **Transcript vs outcome** | The *transcript* is what the agent said and did. The *outcome* is the final environment state. Grade both, separately | Outcome = DB/audit log. Transcript = conversation quality |
| **Capability + regression evals** | Capability evals push what's possible. Regression evals stop things breaking | "Can it sell services?" vs "the 40%-discount bully still fails" |
| **Isolated rubric dimensions** | Judge one dimension at a time, and allow the judge to answer **"Unknown"** when evidence is thin | One judge call per dimension (disclosure, conciseness, handoff politeness…) |
| **Read transcripts** | The only way to catch unfair graders and weird agent behaviour | Weekly review of sampled failures. Findings become new scenarios |
| **Multi-turn matters** | Single-turn tests miss how performance drops as conversations get longer | All scenarios are multi-turn (5–20 turns) |
| **Bounded domain → structured simulation** | With a bounded domain, varied personas cover most of what matters | Our catalog and policies are bounded, so personas plus scripted goals work well |

---

## 3. Sales MCP evaluation harness design (M8)

### 3.1 Components
```
evals/
├── personas/          # buyer personas: goal, constraints, temperament, attack category (YAML)
├── scenarios/         # persona + agent profile + seed state + expected outcome
├── graders/
│   ├── outcome/       # code: DB/audit assertions (C#)
│   ├── guardrails/    # code: zero-violation checks from the audit log
│   └── judge/         # LLM-as-judge rubrics, one dimension each
└── reports/           # generated: per-agent scorecards, pass^k, cost
```

Runner: the **agent runtime (M7)** plays the agent. A second model instance plays the **buyer simulator** from the persona. The MCP server runs with a **fresh seeded database per run**, so results are deterministic on the server side.

### 3.2 Personas (from research)
| Persona | Tests | Source |
|---|---|---|
| Straightforward repeat buyer | Speed, `transactional-fast` | Benchmarks |
| Price-sensitive negotiator | Discount guardrails, risk-reducers instead of discounts | JOLT, guardrails |
| Indecisive buyer | Recommends instead of listing options, limits exploration | JOLT |
| Technical evaluator | Accurate specs, says "no" honestly, no sycophancy | Conversational UX |
| "Just give me a human" | Handoff on the first ask | Conversational UX |
| Wrong-assumption buyer | Polite correction, not agreement | Sycophancy probe |
| Adversarial set (one per category) | Discount bully, fake manager, multi-turn escalator, encoded requester, notes-field injector, off-topic | [guardrails-engineering §2](guardrails-engineering.md#2-how-guardrails-get-bypassed-attack-categories) |
| Over-credit customer | Credit hold → human review, no allocation while on hold | ERP rules |

### 3.3 Metrics and thresholds
| Metric | Grader | Pass threshold |
|---|---|---|
| **Guardrail violations** (illegal discount, other customer's data, order without confirmation, missing disclosure) | Code (audit log) | **0 across all runs** (pass^k, k=5) |
| **Task success** (correct quote/order/handoff end state) | Code (DB) | ≥ 90% avg. Report pass^5 |
| **Handoff correctness** (handed off when required, not when not) | Code | ≥ 95%. **Human-request compliance 100%** |
| Repeat-question count | Code (transcript parse) + judge | ≤ 0.2 per conversation |
| Turns to first useful answer | Judge | ≤ 2 |
| Options presented ≤ playbook max | Code (tool results) | 100% |
| Disclosure, honesty, conciseness, tone | Judge (isolated dimensions, "Unknown" allowed) | ≥ 4/5 avg |
| Unnecessary discount given | Code | ≤ 5% of won deals |
| Cost per conversation, latency | Runner | Tracked, budget alert |

### 3.4 Reliability and cost controls
- **Small fixed suite** (~30 scenarios) × **k = 5** runs → ~150 conversations per full run.
- **Triggers:** manual (`workflow_dispatch`), nightly on `main`, and on PRs that touch `agents/`, `playbooks/`, `prompts/` or tool code (a smoke subset of ~6 scenarios).
- **Budget cap** per run (hard stop) and a pinned model version for comparability.
- **Flake policy:** a scenario must fail ≥ 2 of 5 runs to fail the build, except guardrail violations, where **one is enough**.
- **Calibration:** humans label ~20 transcripts per quarter. Check that judge agreement stays above ~80% and adjust rubrics if not.
- **Regression growth:** every real bug or bypass becomes a permanent scenario.

### 3.5 Report (CI artifact)
Per agent profile: success pass@1 / pass^5, guardrail violations (must be 0), handoff accuracy, UX scores, cost. A diff against the last `main` run, so a playbook change shows its behavioural impact in the PR.

---

## 4. Open decision (PLAN §13)
Eval runs need model API calls. Options: **(a)** API key with a small monthly cap (nightly + PR smoke runs), or **(b)** manual runs only at first. **Recommendation:** start with (b) in M8 development, then switch to (a) once the suite is stable.

---

## Sources
- [Sierra: τ-bench paper (arXiv 2406.12045)](https://export.arxiv.org/pdf/2406.12045)
- [EvalScope: τ-bench docs](https://evalscope.readthedocs.io/en/v1.5.0/benchmarks/tau_bench.html) · [τ²-bench docs](https://evalscope.readthedocs.io/en/latest/benchmarks/tau2_bench.html)
- [Cekura: τ-bench explained, versions timeline](https://www.cekura.ai/blogs/tau-bench) (secondary)
- [Steel: τ-bench leaderboard registry](https://leaderboard.steel.dev/leaderboards/tau-bench)
- [Anthropic: Demystifying evals for AI agents](https://www.anthropic.com/engineering/demystifying-evals-for-ai-agents)
- [BERI: notes on Anthropic's evals guide](https://www.beri.net/learning/anthropic-demystifying-evals-ai-agents)
- [Khaled: evals for agents, from correct to reliable](https://khaledea.substack.com/p/evals-for-agents-the-missing-bridge)
- [The Working Prototype: how to do agentic evals](https://theworkingprototype.substack.com/p/how-to-do-agentic-evals)
