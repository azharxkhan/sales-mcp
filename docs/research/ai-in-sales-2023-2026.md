# AI in Sales, 2023 → 2026: What It Was Used For, What Worked, What Buyers Hated

> Background research for the Sales MCP project. Researched 2026-11-11.
> **On the quarterly view:** no public source publishes per-quarter AI-in-sales metrics. The timeline in §3 is built from **dated events and report releases**, so it is a milestone timeline, not a measured series. Items marked *(bg)* come from general background knowledge and were not re-verified in this pass. Most statistics come from vendor surveys, so treat them as directional.

---

## 1. What sales teams use AI for

| Use case | Evidence |
|---|---|
| **Drafting emails and prospect messages** | The most common use in HubSpot's surveys. Salesforce lists "drafting emails" among the core uses (87% of orgs use AI for something) |
| **Prospecting and lead research** | 34% of teams with agents use them for prospecting. Top sellers are **1.7×** more likely to use prospecting agents (Salesforce 2026) |
| **Defining the ideal customer profile (ICP)** | #1 strategic use, at 46.9% (Belkins, n=120, small sample) |
| **Admin: scheduling, note-taking, CRM entry** | HubSpot. This is the "give reps time back" pitch |
| **Forecasting and lead scoring** | Salesforce and HubSpot |
| **Agent use cases (Salesforce 2026)** | Top agent uses were **order fulfilment, usage tracking and quote creation**. Prospecting came only 5th |
| **Fully autonomous AI SDRs** (AI sales development reps, e.g. 11x, Artisan) | Book meetings end-to-end. The most hyped use, and the least proven (§2) |

**Key gap:** almost everyone "uses AI", but few can show results. Salesloft 2026 (n=500): only **20.6%** say their AI is production-ready with measurable outcomes. RAIN Group: only **20%** use AI tools frequently or daily. Gong's analysis of 7.1M opportunities reportedly found **~95%** of deployments miss the expected commercial impact. The biggest blocker is **data quality** (CRM hygiene, deal visibility), not access to tools.

> **For the MCP:** the agent uses that work best (quotes, order fulfilment, usage tracking) are *structured, data-backed tasks*, and that is exactly what an ERP sales-order MCP does. Good validation of the project idea.

---

## 2. What they tried to get sales with AI, and whether it worked

| Tactic | What happened | Verdict |
|---|---|---|
| **Mass AI-personalised cold email** | Reply rates fell: ~8.5% (2019) → 5% (2025) → 3.4% (2026) by one benchmark. Bridge Group: 3.5% → 1.4% (2018→2025). Belkins: 6.8% → 5.8% (2023→2024). Google/Yahoo bulk-sender rules (Feb 2024) and Microsoft's (May 2025) punish volume and spam complaints. Similar-looking AI templates get flagged as bulk mail | ❌ **Mostly failed.** Volume went up and response collapsed |
| **AI SDRs replacing human SDRs** | TechCrunch (Mar 2025): an anonymous source claimed **70–80%** of 11x customers churned within 3 months (11x disputes this, citing 79% retention). Artisan's CEO admitted first-gen AI SDRs get "a pretty low response rate" and "relatively high churn". Common failure: the AI can't hold a coherent conversation once a prospect replies | ❌ **As a full replacement, failed.** Most buyers moved to hybrid setups |
| **Hybrid: AI for research, drafts and follow-up; humans for replies** | SaaStr ran 5 agents for 6 months with good results, but it needed **15–20 hrs/week of human oversight** and cleaning up its RevOps (sales operations) data first. Salesforce reports a SaaStr case with a 72% open rate and >10% response rate on re-engaging stale contacts (vendor-reported) | ✅ **Works when data is clean and humans supervise** |
| **AI on inbound and existing customers** (qualifying inbound leads, follow-ups, quotes, order status) | Salesforce's top agent uses are post-sale and transactional. Its 2026 agents include Piper (inbound) and Hunter (outbound, still in pilot as of Sep 2026) | ✅ **Best ROI so far:** customers already have intent |
| **Provocative AI marketing** (Artisan "Stop Hiring Humans", Dec 2024) | ~$2M on billboards. National backlash and threats. The slogan was later retired, and the CEO said "the backlash is earned" | ❌ **Brand damage** |
| **Ads and sponsored picks inside AI chat** | 42% dislike chatbot ads. 66% of those say ads make them question the AI's integrity. 67% would trust recommendations less | ❌ **Hurts trust** |
| **Agentic checkout** (buy inside ChatGPT) | ACP Instant Checkout launched Sep 2025. By Mar 2026 OpenAI moved to "discover in chat, buy on the merchant's site" | ⚠️ **Discovery works, in-chat checkout stalled** |

**Pattern:** AI that **adds volume** to outreach failed. AI that **removes friction** for someone who already wants to buy (quotes, stock, order status, follow-up) worked.

---

## 3. Quarter-by-quarter timeline

| Quarter | What AI was used for / what happened | What was working |
|---|---|---|
| **2023 Q1** | ChatGPT boom. Salesforce Einstein GPT and HubSpot ChatSpot announced *(bg)* | Experimentation: email drafting, summaries |
| **2023 Q2–Q4** | AI email writers and call-summary tools spread through sales stacks *(bg)* | Time savings on writing and notes, not revenue |
| **2024 Q1** | **Gmail/Yahoo bulk-sender rules (Feb 2024).** Microsoft Copilot for Sales goes GA *(bg)* | Senders with authenticated, low-volume outreach |
| **2024 Q2** | AI SDR startups raise money and scale (11x, Artisan) | Hype stage. Results not yet visible |
| **2024 Q3** | **Salesforce announces Agentforce** (Dreamforce, Sep) *(bg)* | Agents framed as "digital labour" |
| **2024 Q4** | Agentforce GA *(bg)*. **Artisan "Stop Hiring Humans" billboards** (Dec) → backlash. Cold email reply rates keep falling (Belkins 2024: 5.8%) | Human-first outreach starts to stand out again |
| **2025 Q1** | **TechCrunch exposes AI SDR churn** (11x, Mar) | Disillusionment with fully autonomous SDRs |
| **2025 Q2** | Microsoft bulk-sender rules (May). **Gartner: 61% of buyers prefer rep-free** (Jun) | Self-service and digital buying channels |
| **2025 Q3** | **ChatGPT Instant Checkout / ACP** (Sep 29). Salesforce and Gartner field their 2026 surveys (Aug–Sep) | Buyers using AI to research purchases |
| **2025 Q4** | Artisan loses its LinkedIn presence over data-broker compliance (Dec). Shift to hybrid AI+human | Hybrid setups. AI for research and follow-up |
| **2026 Q1** | **Google/Shopify UCP** at NRF (Jan). **Gartner: 67% rep-free, 45% used AI in a purchase** (Mar). OpenAI drops in-chat checkout (Mar). Salesforce State of Sales 2026: 87% use AI, 54% have used agents | Discover-in-AI, buy-on-merchant-site. Agents on quotes and orders |
| **2026 Q2** | **Amex purchase protection for AI-agent purchases** (Apr). Visa Trusted Agent Protocol live (Q1–Q2). Artisan retires its slogan | Trust and governance infrastructure for agent buying |
| **2026 Q3** | **Salesforce ships 7 role-specific agents** (Sep 11; Hunter outbound still in pilot). Anti-AI billboard graffiti in SF (Sep). Cold email ~3.4% reply rate | Specialised, governed agents with human oversight |
| **2026 Q4** (to date) | Hunter GA expected ~Nov. Agent orchestration and governance tools (Agent Fabric, Guardian) | Too early to call |

**How it moved:** **2023** experiment → **2024** hype and volume → **2025** backlash and correction → **2026** narrow, governed, data-backed agents plus AI-driven buyer discovery.

---

## 4. What people didn't like about AI selling to them

1. **Generic "AI slop" outreach.** People spot templated AI emails fast. Volume plus sameness triggers spam filters *and* buyers' own filters.
2. **Bots that can't hold a conversation.** AI SDRs break down once a prospect actually replies.
3. **Hidden selling inside "neutral" AI.** Ads in chatbots make 66% of those who dislike them question the AI's integrity. 67% would trust the recommendations less, and 57% would penalise the advertising brand.
4. **Brands using AI-generated content.** 32% trust the brand less, and only 7% trust it more (Klaviyo 2026).
5. **Being pushed onto AI instead of a human.** 49% of enterprises expect agents to handle ~78% of service interactions, but **only 19% of consumers** want an AI agent as their main brand touchpoint (Adobe 2026). Older consumers (50+) dislike AI customer service most (2024).
6. **Inconsistent answers.** 69% of B2B buyers see inconsistencies between a seller's website and what reps say (Gartner). AI adds a third source that can contradict both.
7. **The "replace humans" message.** Artisan's billboards turned AI sales into a jobs and ethics issue.
8. **Mixed trust overall.** Trust in AI product recommendations ranges from 35% (Alchemer) to 74% (Salesforce), depending on how the question is asked. ~58% of AI users have been talked *out* of a purchase by a chatbot.

> **For the MCP:** be transparent (label AI actions), keep answers consistent from one data source, make human escalation easy (`request_human_review`), never inject sponsored or upsell bias into tool results, and log every action.

---

## 5. Free reports (downloadable PDFs)

Downloaded locally to [`reports/`](reports/) on 2026-11-11. The PDFs are git-ignored because they are the publishers' copyrighted material, so link to them rather than redistributing them in the public repo.

| Report | Size | Local copy | Original |
|---|---|---|---|
| Salesforce, *State of Sales 2026* (7th ed., 4,050 sales pros) | 3.6 MB | [salesforce-state-of-sales-2026.pdf](reports/salesforce-state-of-sales-2026.pdf) | [source](https://www.salesforce.com/en-us/wp-content/uploads/sites/4/documents/reports/sales/salesforce-state-of-sales-report-2026.pdf) |
| Gong Labs, *State of Revenue AI 2026* (3,048 revenue leaders) | 3.0 MB | [gong-state-of-revenue-ai-2026.pdf](reports/gong-state-of-revenue-ai-2026.pdf) | [source](https://www.gong.io/files/gong-labs-state-of-revenue-ai-2026.pdf) |
| G2, *AI's Impact on Sales 2025* (1,000+ B2B software buyers) | 0.9 MB | [g2-ai-impact-on-sales-2025.pdf](reports/g2-ai-impact-on-sales-2025.pdf) | [source](https://learn.g2.com/hubfs/G2-Blue-Bowen-Insights-Report-AI-Sales-2025.pdf) |
| Mailshake, *Cold Email Report 2025* | 11.1 MB | [mailshake-cold-email-report-2025.pdf](reports/mailshake-cold-email-report-2025.pdf) | [source](https://assets.mailshake.com/wp-content/uploads/2025/04/16091740/Cold-Email-Report-2025-Mailshake.pdf) |

Free but behind a sign-up form (download them yourself): [HubSpot State of Sales 2026](https://offers.hubspot.com/sales-trends-report), [Pipedrive State of Sales & Marketing 2025](https://www.pipedrive.com/en/state-of-sales-and-marketing-2025).
Books from the reading list in [sales-landscape-2026.md](sales-landscape-2026.md) are copyrighted. Buy them or borrow them from a library.

---

## 6. Sources
- [Salesforce, State of Sales 2026: top AI agent use cases](https://www.salesforce.com/news/stories/state-of-sales-report-announcement-2026/top-ai-agent-use-cases/)
- [Salesforce, 40 sales statistics](https://www.salesforce.com/sales/state-of-sales/sales-statistics)
- [HubSpot, AI in B2B sales 2026](https://blog.hubspot.com/sales/ai-b2b-sales?app=1)
- [Belkins, AI for lead generation study](https://belkins.io/blog/ai-for-lead-generation-study)
- [MarketScale, only 20.6% show measurable AI results (Salesloft)](https://marketscale.com/industries/marketing-tech/100-of-revenue-teams-say-they-use-ai-but-only-206-can-show-measurable-results)
- [Mutiny HQ, B2B teams using AI in 2026 (Gong data)](https://www.mutinyhq.com/blog/how-are-b2b-sales-teams-using-ai-in-2026)
- [AI SDR post-mortem: 11x and Artisan](https://deathtocoldemails.com/ai-sdr-post-mortem/)
- [AI SDR's second wave](https://posts.adamgtm.com/p/ai-sdr-second-wave)
- [The CRO Report, 11x vs Artisan](https://thecroreport.com/tools/ai-sdr/11x-vs-artisan/)
- [Amplemarket, AI sales agents compared](https://www.amplemarket.com/blog/best-ai-sales-agents)
- [Dupple, cold email deliverability 2026](https://dupple.com/learn/cold-email-deliverability-2026)
- [Cold email response rate benchmarks](https://deathtocoldemails.pages.dev/cold-email-response-rates)
- [Lilach Bullock, why AI cold emails kill reply rates](https://www.lilachbullock.com/why-ai-cold-emails-are-killing-your-reply-rate/)
- [Gizmodo, "Stop Hiring Humans" outrage](https://gizmodo.com/ai-firms-stop-hiring-humans-billboard-campaign-sparks-outrage-2000536368)
- [KRON4, Artisan retires slogan](https://www.kron4.com/news/technology-ai/ai-startup-behind-stop-hiring-humans-billboards-to-retire-slogan/amp/)
- [SF Standard, anti-AI billboard graffiti (Sep 2026)](https://sfstandard.com/2026/09/02/anti-ai-billboard-graffiti/)
- [Salesforce, Dreamforce 2026 media resources](https://www.salesforce.com/news/dreamforce-26-media-resources/)
- [Lexx Automates, Salesforce's seven job-ready agents](https://www.lexxautomates.com/news/salesforce-agentforce-seven-job-ready-agents/)
- [Semrush, chatbots talked 57.5% of AI users out of buying](https://www.semrush.com/blog/ai-chatbots-talk-ai-users-out-of-buying/)
- [MarTech, AI telling consumers not to buy](https://martech.org/ai-is-telling-consumers-not-to-buy-your-product/)
- [Klaviyo, AI consumer trends 2026](https://www.klaviyo.com/marketing-resources/ai-consumer-trends)
- [Fast Company, trust is a sticking point](https://www.fastcompany.com/91539388/consumers-use-ai-to-shop-but-trust-is-a-sticking-point)
- [F-Secure, consumer AI survey Apr 2026](https://www.f-secure.com/en/partners/illuminate-research/consumer-ai-survey-april-2026)
- [Statista, negative brand perception from AI chatbots by age](https://www.statista.com/statistics/1470244/negative-brand-perception-due-to-ai-chatbots-united-states-by-age-group)
- [Gartner, 61% rep-free (Jun 2025)](https://www.gartner.com/en/newsroom/press-releases/2025-06-25-gartner-sales-survey-finds-61-percent-of-b2b-buyers-prefer-a-rep-free-buying-experience)
- [Demand Gen Report, Gartner 67% rep-free (2026)](https://demandgenreport.com/industry-news/news-brief/gartner-67-of-b2b-buyers-prefer-a-rep-free-experience/52142)
- [OpenAI, Instant Checkout and ACP](https://openai.com/index/buy-it-in-chatgpt/)
