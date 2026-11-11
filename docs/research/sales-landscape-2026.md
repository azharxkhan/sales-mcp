# The State of Sales (Nov 2026): Where It Was, Where It Is, Where It's Heading

> Background research for the Sales MCP project. It covers how selling has changed and what that means for what we build.
> Researched 2026-11-11. Many statistics on the web come from vendor blogs. Where sources disagree, both figures are shown, and primary sources (Gartner, McKinsey, Salesforce) are preferred.

---

## 1. TL;DR

1. **Buyers want to self-serve.** Gartner: **61%** of B2B buyers preferred a rep-free experience (2025 survey). That rose to **67%** in the 2026 survey. **45%** used AI during a recent purchase.
2. **But they still want a human for "does this fit *us*?"** Buyers look for seller input on tasks that need context about their own business. Reps are moving from *information source* to *validator and advisor*.
3. **Omnichannel is the norm.** McKinsey's "rule of thirds": buyers split interactions roughly evenly between **in-person, remote, and digital self-service**. This holds across industries, regions and deal sizes, and most decision makers use **10+ channels**.
4. **AI is everywhere in sales teams, but ROI is behind.** Salesforce's 2026 State of Sales: **87%** of sales orgs use AI, and **54%** of sellers have used AI agents. Gong's analysis of 7.1M opportunities reportedly found **~95%** of AI deployments miss the expected commercial impact.
5. **The newest channel is the agent itself.** "Agentic commerce" protocols (OpenAI/Stripe **ACP**, Google/Shopify **UCP**, Google **AP2**, Visa Trusted Agent Protocol) let AI agents discover and buy for customers. **MCP is the data/tool layer underneath**, and that is exactly what this project builds.
6. **What gets sold has shifted from products to outcomes and recurring revenue.** The subscription economy is estimated at roughly $0.9T–$1.5T in 2026, depending on the source. Focus has moved from *acquiring* subscribers to *retaining* them (Recurly: 52% of consumers cancelled at least one subscription last year).

---

## 2. What used to sell vs. what sells now

| Then (1990s–2010s) | Now (2020s) | Why it changed |
|---|---|---|
| **Products and licences**, bought once (boxed software, perpetual ERP licences, hardware) | **Subscriptions, SaaS, "as-a-service"**, usage-based pricing | Cloud delivery, lower upfront cost for buyers, predictable revenue for sellers |
| Big-ticket purchases decided by one executive | **Buying committees** with many stakeholders | Spend is more scrutinised, and more departments are affected by each tool |
| Electronics and apparel led e-commerce growth | **Grocery, beauty/personal care, home, pet, supplements** lead growth (US grocery e-com reportedly +24–32% YoY). Electronics and apparel are still the largest by revenue | Habitual, replenishable purchases moved online after 2020. Subscriptions and auto-replenish suit them |
| The sale is the finish line | The sale is the start: **renewal, expansion, churn prevention** | Recurring models make the customer's lifetime value the metric that matters |
| Reps controlled product information | Buyers arrive **~already informed** (web, peers, now AI) | The internet, then generative AI |

---

## 3. How the *art* of selling evolved

| Era | Approach | Key idea | Landmark |
|---|---|---|---|
| 1890s–1930s | **Scripted / pitch selling** | Repeatable scripts. AIDA (Attention, Interest, Desire, Action) | NCR's sales scripts. E. St. Elmo Lewis' AIDA (1898) |
| 1930s–1970s | **Relationship selling** | People buy from people they like and trust | Carnegie, *How to Win Friends and Influence People* (1936) |
| 1970s–1980s | **Solution selling** | Diagnose before you prescribe | Frank Watts (Wang Labs). Popularised by Michael Bosworth |
| 1988 | **SPIN Selling** | Evidence-based discovery questions: Situation, Problem, Implication, Need-payoff | Neil Rackham: 35,000 sales calls studied over 12 years |
| 1990s | **Qualification frameworks** | Only spend time on winnable deals | **MEDDIC** at PTC: Metrics, Economic buyer, Decision criteria, Decision process, Identify pain, Champion. Later MEDDPICC |
| 2000s | **Inside sales / Predictable Revenue** | Specialised roles (SDR → AE → CSM), outbound at scale | Aaron Ross, *Predictable Revenue* (Salesforce model) |
| 2011 | **Challenger** | Teach the buyer something new, tailor it, take control | Dixon & Adamson, *The Challenger Sale* |
| 2015–2020 | **Inbound, account-based, customer success** | Content draws buyers in. Target accounts, not leads. Retention becomes a revenue engine | HubSpot inbound. ABM. SaaS customer success |
| 2022 | **Indecision > competition** | Most lost deals end in "no decision", so reduce the buyer's fear of making a mistake | Dixon & McKenna, *The JOLT Effect* |
| 2023–now | **AI-assisted and agentic selling** | AI handles research, outreach and admin. Reps validate, advise and run complex deals. Agents buy from agents | Salesforce/Gartner/McKinsey 2025–26 research. Agentic commerce protocols |

**Practitioner consensus today:** teams combine methods rather than pick one. A typical mix is **SPIN or Gap for discovery, Challenger for engagement, MEDDPICC for qualification and forecasting**. Several analysts argue the frameworks perform about the same, and that *consistent adherence* matters more than which one you choose.

---

## 4. Where it's heading (2026 → 2028)

- **Agent-to-agent commerce.** Buyer-side agents will query seller-side systems for stock, price, terms and fit, then transact. Sellers need **machine-readable catalogues, pricing and policies**. MCP servers are one way to expose them.
- **Reps as validators.** Buyers check what AI told them with a human. Website, AI answers and the rep must say **the same thing** (Gartner: 69% of buyers see inconsistencies today).
- **AI will be measured on revenue outcomes, not adoption.** Expect a correction from "we use AI" to "AI moved win rate / cycle time".
- **Retention economics.** Net revenue retention, churn and expansion matter as much as new bookings.
- **Trust and governance.** Consent, spending limits and audit trails for agent purchases (Amex purchase protection for registered agent purchases, Visa TAP).

---

## 5. What this means for the Sales MCP

| Finding | Design implication |
|---|---|
| Buyers want rep-free self-service | The MCP **is** the self-service channel. Tools like `check_stock`, `get_price`, `create_quote` |
| They still want validation on "fit" | A `prompts/needs-assessment` prompt, plus a `request_human_review` / escalation tool |
| Consistency between channels | One source of truth: tools and resources read the same price list and policy data |
| MEDDPICC is the standard qualification method | The `Deal` entity has MEDDPICC fields, and a `score_deal_qualification` tool |
| Recurring revenue and retention | Order lines support **one-off and subscription** items. An analytics resource for churn / renewals |
| Agentic commerce needs guardrails | Discount approval thresholds, credit-limit checks, spending caps and an audit log of every tool call |
| AI ROI is under scrutiny | Analytics phase: win rate, cycle time, quote→order conversion |

---

## 6. Reading list

### Foundations (classics)
- **Dale Carnegie**: *How to Win Friends and Influence People* (1936). Relationship basics.
- **Robert Cialdini**: *Influence* (and *Pre-Suasion*). The psychology of persuasion.
- **Neil Rackham**: *SPIN Selling* (1988). Research-backed discovery. Matthew Dixon's most-recommended book.

### Modern B2B methodology
- **Matthew Dixon & Brent Adamson**: *The Challenger Sale* (2011) and *The Challenger Customer* (2015).
- **Matthew Dixon & Ted McKenna**: *The JOLT Effect* (2022). Winning against "no decision".
- **Keenan**: *Gap Selling*. Current state → future state discovery.
- **John McMahon**: *The Qualified Sales Leader*. MEDDIC from one of its originators.
- **Aaron Ross & Marylou Tyler**: *Predictable Revenue*. The SDR/AE pipeline model.
- **Mark Roberge**: *The Sales Acceleration Formula*. Data-driven sales teams (HubSpot).

### Prospecting, objections and negotiation
- **Jeb Blount**: *Fanatical Prospecting* and *Objections*.
- **Chris Voss**: *Never Split the Difference*. Negotiation, the most widely recommended title.
- **Daniel Pink**: *To Sell Is Human*. Why everyone sells.

### Reports and documentation (primary sources, refreshed yearly)
- Salesforce: *State of Sales* (2026 edition, ~4,000 sales pros)
- Gartner: B2B buying surveys, [61% rep-free (Jun 2025)](https://www.gartner.com/en/newsroom/press-releases/2025-06-25-gartner-sales-survey-finds-61-percent-of-b2b-buyers-prefer-a-rep-free-buying-experience) and [67% rep-free (Mar 2026)](https://demandgenreport.com/industry-news/news-brief/gartner-67-of-b2b-buyers-prefer-a-rep-free-experience/52142)
- McKinsey: [B2B Pulse, "The new B2B growth equation"](https://www.mckinsey.com/capabilities/growth-marketing-and-sales/our-insights/the-new-b2b-growth-equation) and [Five fundamental truths](https://www.mckinsey.com/capabilities/growth-marketing-and-sales/our-insights/five-fundamental-truths-how-b2b-winners-keep-growing)
- HubSpot: [State of Sales 2026](https://offers.hubspot.com/sales-trends-report)
- Recurly: [State of Subscriptions 2026, 76M subscribers](https://recurly.com/blog/analyzing-trends-of-76-million-subscribers/)
- OpenAI: [Instant Checkout and the Agentic Commerce Protocol](https://openai.com/index/buy-it-in-chatgpt/)
- Wikipedia: [Agentic commerce](https://en.wikipedia.org/wiki/Agentic_commerce) (good overview of ACP / UCP / AP2)
- Model Context Protocol docs: https://modelcontextprotocol.io

---

## 7. Sources used
- [Gartner press release, Jun 2025](https://www.gartner.com/en/newsroom/press-releases/2025-06-25-gartner-sales-survey-finds-61-percent-of-b2b-buyers-prefer-a-rep-free-buying-experience)
- [Demand Gen Report, Gartner 67% (2026)](https://demandgenreport.com/industry-news/news-brief/gartner-67-of-b2b-buyers-prefer-a-rep-free-experience/52142)
- [Distribution Strategy, Mar 2026](https://distributionstrategy.com/2026/03/most-b2b-buyers-prefer-rep-free-purchasing-as-ai-reshapes-sales/)
- [McKinsey B2B Pulse](https://www.mckinsey.com/capabilities/growth-marketing-and-sales/our-insights/the-new-b2b-growth-equation)
- [McKinsey, survival threshold of B2B growth](https://www.mckinsey.com/br/en/our-insights/the-surprising-economics-of-b2b-growth-the-new-survival-threshold-and-what-it-takes-to-thrive)
- [r-sun.ai, AI-driven B2B sales 2026 (cites Salesforce)](https://r-sun.ai/insights/ai-driven-b2b-sales-2026)
- [Mutiny HQ, B2B sales teams using AI in 2026 (cites Gong)](https://www.mutinyhq.com/blog/how-are-b2b-sales-teams-using-ai-in-2026)
- [Tommaso Maria Ricci, AI for sales](https://www.tommasomariaricci.com/blog/ai-for-sales-guide)
- [David Craig White, State of B2B Sales 2026](https://davidcraigwhite.com/the-state-of-b2b-sales-2026/)
- [ECDB, e-commerce growth winners 2026](https://ecdb.com/blog/growth-winners-and-losers-among-subcategories-in-2026/5152)
- [Darkroom, Amazon categories growing in 2026](https://www.darkroomagency.com/observatory/which-amazon-product-categories-are-growing-in-2026)
- [Supered, sales methodologies guide](https://www.supered.io/guide/sales-methodologies/)
- [Salesfolks, selling systems revisited](https://salesfolks.com/post/selling-systems-revisited)
- [Rye, agentic commerce landscape](https://rye.com/blog/agentic-commerce-startups)
- [Recurly, 76M subscribers analysis](https://recurly.com/blog/analyzing-trends-of-76-million-subscribers/)
- [Summary.com, 25 best sales books](https://www.summary.com/?p=7389)
- [Goodreads, best sales books shelf](https://www.goodreads.com/shelf/show/best-sales-books)
