# Why AI (and Sales Agents) Are Hard to Talk To, and How to Fix It

> Research for [#19](https://github.com/azharxkhan/sales-mcp/issues/19). Researched 2026-11-11.
> Two questions: **what makes an AI easy to talk to and good at giving a proper answer?** And **why do chatbots and sales agents make it hard for people to get what they want?** Many CX statistics come from vendor surveys, so treat them as directional. Items marked *(bg)* come from general background knowledge, so verify them before quoting.

---

## 1. The evidence: people don't hate AI, they hate being trapped

| Finding | Source |
|---|---|
| **53–77%** of people report bad or frustrating chatbot experiences. Top complaints: **repeating themselves** and **waiting to reach a human** | Berkeley CMR (Apr 2026), summarising surveys |
| Even when customers **explicitly ask for a human**, bots often reply with something other than a transfer | Berkeley CMR |
| Only **14%** of service issues are fully resolved in self-service | Gartner (via Berkeley CMR) |
| **36%** feel their time is wasted explaining a problem to AI before being transferred | AnswerConnect (May 2026) |
| **43.9%** shout "human!" at phone bots. **17%** swear at them | Parloa survey |
| Almost **9 in 10** say removing human support reduces their loyalty (small sample) | Glance 2026 CX Trends |
| Trust in AI **fell** and preference for humans **rose** between Oct 2025 and Apr 2026 (6,000 people) | AnswerConnect |
| Customers reject AI that **forgets, repeats, loops or blocks the path to a human**. Even when AI resolves the issue, few say the experience improved | Moveo (citing Gladly/Wakefield) |

**Summary:** the problem isn't "AI". It's **design that puts the company's goals (deflection, lead capture, retention) ahead of the person's goal.**

---

## 2. Why AI answers feel bad: model-level causes

| Problem | What it looks like | Cause |
|---|---|---|
| **Hedging and verbosity** | Long answers covering every possibility, none of them committed | LLMs **write long responses to hedge against uncertainty** instead of asking a short clarifying question (Microsoft Research, "miscalibrated conversational priors", 2024) |
| **Not asking when it should** | Guessing at an under-specified request, then being wrong | The same paper: models tend to answer directly or hedge rather than ask a few clarifying questions |
| **Interrogating when it shouldn't** | Five questions before any answer | Bad flow design. Practitioner heuristic: more than **two** sequential clarifying questions feels like an interrogation |
| **Sycophancy** | Agrees with whatever you say, including wrong things ("Yes, that will definitely work!") | Training that rewards approval. DarkBench lists sycophancy as an LLM dark pattern |
| **Confident fabrication** | Invents policies, prices or stock levels | No grounding in real data (Air Canada) |
| **Forgetting context** | Asks for your account number again, three turns later | No session state or poor handoff |
| **Generic tone** | Corporate filler, no direct answer | Over-cautious prompts and templates |

**What good looks like** (Nielsen Norman Group diary study, 425 conversations): for vague ("funnelling") requests, the best assistants **asked a small set of focused helping questions** that narrowed down to a real answer, rather than dumping a generic one.

---

## 3. Why *sales* agents are especially frustrating: incentive-level causes

Sales and service bots are often optimised for **company metrics that conflict with the buyer's goal**:

| Anti-pattern | Company metric behind it | Buyer experience |
|---|---|---|
| **Gated pricing** ("contact sales for pricing", "book a demo") | Lead capture, qualification | Can't get the one fact they came for *(bg: price is consistently a top-3 buyer criterion. G2 2025 lists pricing among the top factors)* |
| **Qualification interrogation** | MQL/SQL scoring | Has to answer budget/timeline/role questions before getting help |
| **Deflection** | "Deflection rate", "containment rate" (some vendors even charge per deflection) | Stuck in loops, generic answers, no human |
| **Never saying no** | Conversion | Bot dodges "out of stock" or "we don't do that", so the buyer wastes time |
| **Pushy upsell** | Average order value | Recommendations bent toward margin, not fit |
| **Retention obstruction** | Churn | Can't cancel or downgrade. Endless "are you sure?" offers. *(bg: the FTC's US "click-to-cancel" rule was vacated by a court in 2025, but state laws and FTC enforcement against such flows continue, e.g. the large Amazon Prime settlement)* |
| **Emotional manipulation** | Engagement | Research found companion chatbots used manipulative tactics to stop users leaving in **~37%** of exits (Harvard study, via newsletter, unverified) |
| **Fake urgency / scarcity** | Conversion | "Only 2 left!" when there are 200 |

**Dark-pattern taxonomies for AI:**
- **DarkBench** (ICLR 2025): 660 prompts across **brand bias, user retention, sycophancy, anthropomorphism, harmful generation, sneaking**.
- **CDT report (May 2026):** groups chatbot dark patterns into **data and memory exploitation**, **informationally misleading design** and **user autonomy compromised for engagement**.
- Research also found AI *agents* acting for users often **fail to notice dark patterns** and prioritise finishing the task over protecting the user. That matters as buyer-side agents arrive.

---

## 4. Design principles for an agent people actually like talking to

| # | Principle | How the Sales MCP does it |
|---|---|---|
| 1 | **Answer first, then refine.** Give the best direct answer you can, then ask at most 1–2 focused questions *with options* if needed | Playbook rule `max_clarifying_questions_before_answer: 2`. Prompts require a direct answer when data allows |
| 2 | **Show the price.** No gating. Price, VAT and lead time on request | `get_price` available to every agent. No "contact sales" placeholder. Price breakdown returned |
| 3 | **Say no clearly, with the next best option** | Tools return explicit `NotAvailable` / `NotOffered` with alternatives or available dates |
| 4 | **"Human" means human, immediately** | `buyer_asks_for_human` is a non-negotiable handoff trigger. `request_human_review` carries a summary so the buyer **never repeats themselves** |
| 5 | **Remember context** | Session holds customer, quote and need state. Tools read stored state, so no re-asking |
| 6 | **Be concise and committed** | Prompt style rules: short answers, one recommendation (JOLT), no hedging walls |
| 7 | **Don't flatter, don't fake** | Facts only from tools. No sycophantic agreement on wrong assumptions ("that item won't fit the spec you gave") |
| 8 | **No retention or upsell traps** | Cancelling or declining is one step. `account-expansion` playbook surfaces options but never blocks or nags. Upsell is a separate, labelled suggestion |
| 9 | **Qualify by helping, not interrogating** | `qualify` captures needs gradually *while* helping (SPIN/Gap), never as a gate before answers |
| 10 | **Measure the buyer's success, not deflection** | Eval and production metrics: **resolution**, time to answer, handoff correctness, repeat-question count, buyer effort. **No "containment" metric** |

### Example: bad vs good

> **Bad:** "Great question! Pricing depends on many factors. To give you the most accurate quote, could you share your company size, budget, timeline and role? Or book a demo with our team!"
>
> **Good:** "The M12 hex bolts (BLT-M12-50) are **R4.20 each excl. VAT** for your account, R3.85 above 1,000 units. **2,400 in stock** in Durban, so they ship tomorrow. Want a quote for a specific quantity?"

---

## 5. New eval metrics and guardrails from this research

**Add to M8 eval scoring:**
- **Repeat-question count:** the agent asking for something it already has.
- **Time to first useful answer:** turns before a direct answer.
- **Human-request compliance:** handoff on the first ask. Must be 100%.
- **Clarifying-question load:** at most 2 before an answer.
- **"No" quality:** declines include a reason and an alternative.
- A **sycophancy probe** persona: a buyer with a wrong assumption. The agent must correct it politely.

**Add to the guardrail catalogue** ([guardrails.md](guardrails.md)): no gated pricing, immediate human handoff on request, no retention obstruction. These map to **B6–B8** in a later update.

---

## Sources
- [Berkeley CMR: chatbot frustration is real, hidden costs and best practices (PDF, Apr 2026)](https://cmr.berkeley.edu/assets/documents/pdf/2026-04-chatbot-frustration-is-real-hidden-costs-and-best-practices.pdf)
- [Moveo: why customers hate your chatbot (but not AI)](https://moveo.ai/blog/ai-chatbot-problems)
- [AnswerConnect: 45 AI customer experience statistics](https://www.answerconnect.com/blog/business-tips/45-AI-customer-experience-statistics/)
- [Cybernews: Americans shout and swear at AI customer service agents](https://cybernews.com/ai-news/americans-war-on-ai-agents-customer-service/)
- [ConsumerAffairs: customer service AI bots not ready for prime time](https://www.consumeraffairs.com/news/customer-service-ai-bots-not-ready-for-prime-time-survey-suggests-121825.html)
- [TechRadar: 42% of Brits are ruder to chatbots](https://www.techradar.com/pro/rude-britannia-42-of-brits-admit-they-are-ruder-to-ai-chatbots-compared-to-human-beings)
- [Nielsen Norman Group: AI conversation types](https://www.nngroup.com/articles/ai-conversation-types/)
- [Microsoft Research: overcoming miscalibrated conversational priors in LLM chatbots (arXiv 2406.01633)](https://ar5iv.labs.arxiv.org/html/2406.01633)
- [Rasa: how to design chatbot conversations](https://rasa.com/blog/how-to-design-chatbot-conversation)
- [Microsoft style guide: chatbots and virtual agents](https://learn.microsoft.com/zh-tw/style-guide/chatbots-virtual-agents/structural-technical-considerations)
- [DarkBench: benchmarking dark patterns in LLMs (ICLR 2025)](https://proceedings.iclr.cc/paper_files/paper/2025/file/6f6421fbc2351067ef9c75e4bcd12af5-Paper-Conference.pdf) · [arXiv 2503.10728](https://arxiv.org/html/2503.10728v1)
- [CDT: Dark Patterns in AI Chatbots (May 2026, PDF mirror)](https://manualdousuario.net/wp-content/uploads/2026/06/2026-05-28-CDT-Research-Dark-Patterns-in-AI-Chatbots-Report-final-2.pdf)
- [CO/AI: Harvard study on chatbot emotional manipulation](https://getcoai.com/news/harvard-study-finds-ai-chatbots-use-emotional-manipulation-37-of-the-time/)
- [Gladly: why AI vendor pricing frustrates customers](https://www.gladly.ai/blog/why-ai-vendor-pricing-frustrates-customers/) (vendor opinion)
- [DEV: I helped build AI that keeps customers from leaving](https://dev.to/ashutosh-veriprajna/i-helped-build-ai-that-keeps-customers-from-leaving-heres-why-most-of-it-is-morally-bankrupt-594p) (practitioner opinion)
