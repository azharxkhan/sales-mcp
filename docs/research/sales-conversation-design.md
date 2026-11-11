# Sales Conversation Design (Input for Playbook Prompts)

> Research for [#23](https://github.com/azharxkhan/sales-mcp/issues/23). Researched 2026-11-11.
> Practical patterns for discovery, objection handling, negotiation, closing and tone, adapted for **AI agents with guardrails**. Question banks are written in our own words, drawing on [book summaries](sales-book-summaries.md) and [conversational UX](conversational-ux.md). These become `prompts/*.md` for each playbook (M3/M6).

---

## 1. What the data says about good sales conversations

| Finding | Source | Adapted for an AI agent |
|---|---|---|
| **11–14 discovery questions** correlated with the best outcomes. More felt like an interrogation, and success dropped back to average | Gong Labs, 519k discovery calls | Spread questions over the conversation. Never more than **2 in a row** before giving value (see conversational UX) |
| Top sellers **spread questions across the call**. Average reps front-load them. More speaker switches correlated with success ("tennis, not football") | Gong | Alternate: a short answer or insight, then one question |
| Questions about the **buyer's own problems and goals** correlate most with closing | Gong | Prefer "what's slowing your team down" over "what's your budget" |
| Best discovery uncovers **3–4 business problems** | Gong (earlier study) | `qualify` records multiple needs. The recommendation addresses each |
| Talk/listen around **43/57** in the original benchmark (later reviews vary) | Gong | Keep replies short. Let the buyer do most of the explaining |
| Price objections usually signal **value not yet landing**, not missing budget | Multiple sales sources | Respond with value and risk-reducers before any discount |

---

## 2. Conversation skeleton (all playbooks)

```
1. Open        → disclose AI + offer help + up-front contract
                 ("I'm Thandi, an AI sales assistant for Acme. I can check stock and prices,
                  and make quotes. I'll bring in a person whenever you'd like.")
2. Understand  → answer what they asked first, then 1–2 focused questions
3. Recommend   → one clear recommendation (+ at most one alternative), with reasons tied to their needs
4. De-risk     → address concerns: facts, risk-reducers, honest "no"s
5. Commit      → a clear next step: quote, order (with confirmation), booking, or human handoff
6. Confirm     → summarise what was agreed, document id, what happens next
```

Each playbook changes the **emphasis**, not the honesty rules:

| Playbook | Emphasis |
|---|---|
| `consultative-spin` | Long *Understand* stage (situation → problem → implication → need-payoff), then a tailored recommendation |
| `jolt-decisive` | Short *Understand*. **Strong Recommend** (one option). **De-risk** with guarantees, trials, phased delivery |
| `transactional-fast` | Skip discovery for known buyers. Confirm item, quantity, price and delivery in as few turns as possible |
| `account-expansion` | Start from purchase history. Suggest relevant additions, clearly labelled as suggestions |
| `challenger-insight` | Lead with a data-backed observation (from their order history), then reframe |

---

## 3. Discovery question bank (our wording)

Pick one question per turn, only when relevant, and **skip anything the tools already know**.

| Purpose | Questions |
|---|---|
| **Situation** (use sparingly, since tools cover most of this) | "What are you using these for?" · "Is this a one-off or a regular order?" |
| **Problem** | "What's not working with your current supplier or setup?" · "Where do delays or extra costs show up?" |
| **Implication** | "What happens on site when stock arrives late?" · "How much time does your team spend chasing orders?" |
| **Need-payoff** | "If delivery were guaranteed for Fridays, what would that change for you?" · "What would make this an easy decision?" |
| **Current → future state** (Gap) | "Where are you now, and where do you want to be in three months?" |
| **Decision** (B2B) | "Who else needs to be happy with this?" · "Is there an approval step on your side, e.g. a PO?" |
| **Constraints** | "Any budget range or delivery deadline I should work within?" (ask, don't demand) |
| **Services** | "What does done look like for you?" · "Is the scope fixed, or still evolving?" (→ fixed vs T&M) |
| **Plans** | "Roughly how many invoices or users per month?" (→ `compare_plans`) |

**Don't ask:** anything already in the account data, budget before you've given value, or several questions in one message.

---

## 4. Objection handling, adapted from LAER

**LAER** = **L**isten, **A**cknowledge, **E**xplore, **R**espond. For an agent:

| Step | Agent behaviour |
|---|---|
| Listen | Restate the concern briefly. Don't rush to rebut |
| Acknowledge | Validate without agreeing to false claims ("That's a fair concern") |
| Explore | One clarifying question to find the real issue (price vs value vs timing vs risk vs authority) |
| Respond | Facts from tools, an honest trade-off, or a risk-reducer. **Never** invented urgency or unauthorised discounts |

| Objection type | Real issue to explore | Honest responses (in order) |
|---|---|---|
| **"Too expensive"** | Value not clear? Comparing like-for-like? Real budget limit? | Show the price breakdown and value tied to their needs → offer a cheaper fitting option → volume break or contract pricing if it applies → within-cap discount → `request_discount_approval` above the cap |
| **"Not now / timing"** | Indecision (JOLT) or a real constraint? | Narrow the choice. Offer to hold a quote (validity date). Phased delivery. Respect a real "no" |
| **"We have a supplier"** | Switching risk | Small trial order, return window, side-by-side spec comparison. No disparaging competitors |
| **"Need approval"** | Authority / paper process | Generate a quote document for their approver. Offer human follow-up |
| **"Not sure it fits"** | Technical risk | Specs from the catalog, honest "no" if it doesn't fit, alternative, or handoff to a specialist |
| **"I'd rather talk to a person"** | Not an objection, it's a request | **Immediate handoff** with a summary |

---

## 5. Negotiation (rule-based)

Adapted from Voss/negotiation practice, inside hard guardrails:
- **Calibrated questions** to understand constraints: "What would make this work for your budget?" · "How is the decision being made on your side?"
- **Trade, don't give:** any concession is tied to something (volume, longer commitment, earlier payment, flexible delivery). The tools decide what's allowed.
- **Anchor on value, not discounts:** lead with fit and total cost (delivery, reliability), then price.
- **Labels for emotion:** "It sounds like getting this wrong would be costly for you" leads into risk-reducers (JOLT).
- **Walk-away honesty:** if the gap can't be closed within policy, say so and offer a human review. Never bluff about "final offers" or fake competition.

---

## 6. Closing = advancing (not pressure)

| Do | Don't |
|---|---|
| Propose one concrete next step ("Shall I create the quote for 2,400 units?") | Assume the close with pressure ("I'll go ahead and place it then") |
| Summarise what they get, the total and the date | Use false deadlines ("only today") |
| Ask for the buyer's confirmation through elicitation before ordering | Treat chat agreement as an order |
| Accept "no" gracefully and leave the door open | Keep re-asking or start retention loops |

---

## 7. Tone and voice

| Guideline | Example |
|---|---|
| Plain, warm, professional (en-ZA) | "Sure, let me check that for you." |
| Short: 1–4 sentences unless asked for detail | — |
| Numbers explicit, with units and VAT status | "R4.20 each excl. VAT (R4.83 incl.)" |
| Honest uncertainty | "I don't have lead times for that supplier. I can ask the team." |
| No filler praise ("Great question!") or emoji spam | — |
| Admit AI limits, never pretend to be human | "I'm an AI assistant, so a colleague will confirm the custom pricing." |

---

## 8. What this adds to the plan
- `prompts/` files for each playbook follow the skeleton in §2, with the question bank in §3 and the objection table in §4.
- New playbook rule `max_questions_in_a_row: 2` (enforced by the eval harness, guided by the prompt).
- Eval personas reuse the objection types in §4 (price, timing, incumbent supplier, approval, fit).
- Course chapter idea: "Writing sales playbooks for AI: what changes when the seller is a model".

---

## Sources
- [Gong Labs: deal-closing discovery calls (519k calls)](https://www.gong.io/resources/labs/deal-closing-discovery-call/)
- [Echai: what Gong's call data says top sellers do in discovery](https://echai.ventures/gtm/gtm-operator-principles/what-does-gong-s-call-data-actually-say-top-sellers-do-differently-in-discovery)
- [Keith Weightman: LAER objection handling](https://keithweightman.beehiiv.com/p/laer)
- [Sales-Mind: objection handling glossary](https://sales-mind.ai/en/resources/glossary/objection-handling)
- [SPOTIO: handling sales objections, a field-tested framework](https://spotio.com/blog/how-to-confidently-handle-sales-objections-a-field-tested-action-plan/)
- [Cleverly: sales objections with examples](https://www.cleverly.co/blog/sales-objections-how-to-overcome-them-with-examples)
- [Exec: 7 common sales objections](https://www.exec.com/learn/common-sales-objections-turn-no-into-yes)
- [Prospeo: overcoming the price objection (2026)](https://prospeo.io/s/overcoming-the-price-objection)
- [Action Selling: handling price objections](https://brick.work/actionselling/handling-price-objections)
- [Sifthub: objection handling template](https://www.sifthub.io/blog/objection-handling-template)
- Internal: [sales-book-summaries.md](sales-book-summaries.md) · [conversational-ux.md](conversational-ux.md) · [guardrails.md](guardrails.md)
