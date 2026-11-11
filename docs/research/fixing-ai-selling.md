# How Fixing the AI-Selling Complaints Would Have Worked

> Follow-up to [ai-in-sales-2023-2026.md §4](ai-in-sales-2023-2026.md). For each buyer complaint: the fix, the evidence that it works, a real case where it was missing, and how the Sales MCP builds it in.
> Researched 2026-11-11. "Would have worked" is a reasoned counterfactual from the evidence below, not a controlled experiment. Vendor-commissioned surveys are flagged.

---

## Summary

| Complaint | Fix | Strongest evidence | Case where it was missing |
|---|---|---|---|
| Generic AI-written emails | Fewer, research-backed messages, sent to people who already show intent | Reply rates fell as AI volume rose (≈8.5% → 3.4%). Agents do best on quotes, orders and inbound | Mass AI SDR outreach (11x, Artisan): high churn |
| Bots break down once you reply | Hand off to a human as soon as the AI is out of its depth, **with context passed along** | Trust in AI resolving an issue: **55% with a reachable human vs 26% without** (Five9, vendor survey) | Klarna (2024–25): an AI-first push "went too far", and the company started hiring humans again |
| Hidden ads inside answers | No paid placement or upsell bias in answers. Label anything sponsored | 42% dislike chatbot ads. 66% of those question the AI's integrity. 67% would trust picks less | Ads inside AI chat drove the biggest trust drop in consumer surveys |
| Being pushed onto a bot | AI as an option, not a gate. "Talk to a human" always visible | Only 19% of consumers want an AI agent as their main contact with a brand (Adobe 2026) | Klarna's reversal, and more generally CX teams that tried to deflect customers away from humans |
| AI content lowers trust | Disclose AI use, and make AI output accurate and consistent with company policy | Now law: **EU AI Act Art. 50** requires chatbot disclosure from **2 Aug 2026** (fines up to €15M / 3%) | **Air Canada (Feb 2024):** its chatbot invented a refund policy, and the tribunal held the airline liable |
| "AI should replace people" | Position AI as giving reps time back, so humans stay in the loop | Hybrid setups worked (SaaStr: 5 agents plus 15–20 hrs/week of human oversight) | Artisan's "Stop Hiring Humans" billboards: ~$2M on ads, a national backlash, and the slogan was retired |

---

## 1. Generic AI emails → relevance over volume

**What the fix looks like:** send fewer messages, triggered by real signals (an inbound enquiry, a quote that's about to expire, a back-order that's now in stock). Write them from the customer's actual data rather than a template that thousands of others received.

**Why it would have worked:**
- Spam filters score **sameness across senders** and complaint rates (Gmail/Yahoo from Feb 2024, Microsoft from May 2025). Low-volume, specific messages avoid both.
- Salesforce 2026: the top uses for AI agents are **order fulfilment, usage tracking and quote creation**, all moments where the customer already wants to hear from you.
- Salesforce reports a SaaStr agent re-engaging contacts that hadn't been followed up for six months: a 72% open rate and >10% response rate (vendor-reported). That's targeted follow-up, not a cold blast.

**Sales MCP:** no "blast" tools at all. Follow-ups come from events (`/order-follow-up`, back-order-filled, quote-expiring) and are grounded in order data.

---

## 2. Bots that can't hold a conversation → escalation with context

**What the fix looks like:** a clear rule for when the AI hands over (confidence, topic, the customer asking, any exception). The human gets the full history, so the customer doesn't repeat themselves.

**Why it would have worked:**
- Five9 (3,000 consumers, US/UK/DE; vendor-commissioned): trust that AI will resolve an issue is **55% when a human is reachable vs 26% when not**. 92% would escalate within 10 minutes. About a third had to **repeat themselves** after a handoff, so passing context along is the missing piece.
- Independent evidence on the *assistive* model: an NBER field study of 5,179 support agents found an AI assistant raised issues resolved per hour by **14%**. That's AI helping humans, not replacing them.
- **Klarna:** in Feb 2024 it claimed its AI did the work of ~700 agents. By May 2025 its CEO said cost had been weighted too heavily, leading to "lower quality", and Klarna started piloting human agents again so customers always have a human option.

**Sales MCP:** a `request_human_review(entityId, reason)` tool that attaches the quote/order and the conversation summary to a review item. Business rules force escalation automatically (discounts over 10%, credit holds, cancellations).

---

## 3. Hidden ads → neutral, labelled results

**What the fix looks like:** recommendations ranked only by fit (stock, price, customer history). Anything promoted is labelled. No commissions shaping the answer.

**Why it would have worked:** Semrush (2,338 US consumers): 42% dislike chatbot ads, and 66% of those say ads make them question the AI's integrity. Another survey: 67% would trust recommendations less, and 57% would penalise the advertising brand. Neutral answers keep trust, and ~58% of AI users *have* bought on a chatbot's recommendation, so trusted AI does convert.

**Sales MCP:** tool results return facts (price, stock, lead time) with no "recommended upsell" field. If cross-sell is ever added, it goes in a separate, labelled tool.

---

## 4. Forced onto a bot → AI as an option

**What the fix looks like:** the customer chooses the channel. This matches McKinsey's "rule of thirds": buyers split their contact roughly equally between in-person, remote and self-service.

**Why it would have worked:** only 19% of consumers want an AI agent as their main contact (Adobe 2026), but 67% of B2B buyers prefer rep-free *buying* (Gartner 2026). People want self-service on their own terms, not a bot standing between them and a human.

**Sales MCP:** the server is one channel among several. Its data model never assumes the AI is the only way in. Orders created by AI or humans go through the same rules.

---

## 5. Untrustworthy AI content → disclosure plus one source of truth

**What the fix looks like:** say clearly that it's AI. Make the AI answer from the **same policy and price data** the website and reps use, never from free-form guesses.

**Why it would have worked:**
- **Air Canada (BC Civil Resolution Tribunal, 14 Feb 2024):** the chatbot told a grieving customer they could claim a bereavement refund after travel. The real policy said otherwise. Air Canada argued the chatbot was a "separate legal entity". The tribunal rejected that and ordered the refund plus fees. If the bot had read the actual policy instead of generating one, the case wouldn't have existed. (Experts also noted a disclaimer might have limited liability, but accuracy is the real fix.)
- Gartner: 69% of B2B buyers already see inconsistencies between websites and reps. An AI with its own version of the truth makes that worse.
- **Regulation now requires disclosure:** EU AI Act Article 50 requires people to be told they're talking to AI, at the latest at the first interaction, from 2 Aug 2026.

**Sales MCP:** policies are exposed as **resources** (`policy://discounts`, `pricelist://{id}`) that tools read from. The tools *compute* prices and eligibility rather than letting the model guess. Mutating tool results state "performed by AI agent on behalf of {client}". Every action is audited.

---

## 6. "Replace people" → augment people

**What the fix looks like:** frame and design AI as taking over admin, research and drafts, so reps spend more time with customers.

**Why it would have worked:** the setups that delivered results (SaaStr's 5 agents) needed 15–20 hours a week of human oversight. Teams that tried full replacement mostly went back to hybrid. Artisan spent ~$2M on "Stop Hiring Humans" billboards, got threats and a national backlash, and later retired the slogan, with its CEO saying the backlash was "earned".

**Sales MCP:** the README frames the project as "a rep's co-pilot and a self-service channel". Approval flows (`request_discount_approval`) put humans in the loop by design.

---

## Sources
- [Five9 survey: consumers want a human backup](https://letsdatascience.com/news/five9-survey-finds-consumers-want-a-human-backup-for-ai-serv-04c7b107)
- [Fortune: Klarna, AI, humans and ROI](https://fortune.com/2025/05/09/klarna-ai-humans-return-on-investment)
- [Maginative: Klarna dials back AI customer service](https://www.maginative.com/article/klarna-dials-back-its-ai-customer-service-strategy-now-its-hiring-humans-again/)
- [Entrepreneur: Klarna hiring agents again](https://www.entrepreneur.com/business-news/klarna-ceo-reverses-course-by-hiring-more-humans-not-ai/491396)
- [Skift: Air Canada ordered to pay for chatbot's false info](https://skift.com/2024/02/16/air-canada-ordered-to-pay-for-its-chatbots-false-info)
- [Manatt: airline must honour a policy its chatbot fabricated](https://manatt.com/insights/newsletters/advertising-law/ai-gone-wild-airline-has-to-honor-a-refund-policy)
- [CX Today: Air Canada chatbot ruling](https://www.cxtoday.com/conversational-ai/court-orders-air-canada-to-pay-out-for-chatbots-bad-advice/)
- [Secure Privacy: EU AI Act Article 50](https://secureprivacy.ai/blog/eu-ai-act-article-50-transparency-obligations-for-chatbots-and-deepfakes-2026)
- [Mondaq: Commission guidelines on transparency](https://www.mondaq.com/uk/privacy-protection/1825278/european-commission-press-release-eu-ai-act-transparency-enforcement-starts-this-sunday-do-you-know-who-your-chatbots-are-we-break-down-the-commissions-guidelines-on-transparency-below)
- [Semrush: chatbots and buying decisions](https://www.semrush.com/blog/ai-chatbots-talk-ai-users-out-of-buying/)
- [Salesforce State of Sales 2026: agent use cases](https://www.salesforce.com/news/stories/state-of-sales-report-announcement-2026/top-ai-agent-use-cases/)
- [Salesforce Dreamforce 2026 media resources](https://www.salesforce.com/news/dreamforce-26-media-resources/)
- [AI SDR post-mortem](https://deathtocoldemails.com/ai-sdr-post-mortem/)
- [KRON4: Artisan retires slogan](https://www.kron4.com/news/technology-ai/ai-startup-behind-stop-hiring-humans-billboards-to-retire-slogan/amp/)
- [Gartner: rep-free buying (2025)](https://www.gartner.com/en/newsroom/press-releases/2025-06-25-gartner-sales-survey-finds-61-percent-of-b2b-buyers-prefer-a-rep-free-buying-experience)
- [McKinsey B2B Pulse](https://www.mckinsey.com/capabilities/growth-marketing-and-sales/our-insights/the-new-b2b-growth-equation)
