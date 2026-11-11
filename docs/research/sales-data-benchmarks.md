# Sales Data and Benchmarks (2024–2026)

> Research for [#11](https://github.com/azharxkhan/sales-mcp/issues/11). Researched 2026-11-11.
> **Primary** = figures read directly from the four reports downloaded locally (Salesforce *State of Sales 2026*, Gong Labs *State of Revenue AI 2026*, G2 *AI's Impact on Sales 2025*, Mailshake *Cold Email Report 2025*). See [ai-in-sales-2023-2026.md §5](ai-in-sales-2023-2026.md#5-free-reports-downloadable-pdfs) for links. Everything else is secondary and often vendor-run. Benchmarks vary widely with definitions (e.g. whether "no decision" counts as a loss), so treat them as **ranges, not targets**.

---

## 1. Headline numbers

| Metric | Value / range | Source | Quality |
|---|---|---|---|
| **New-business win rate** | ~19–21% (top performers 30%+) | Ebsta 2025, HubSpot, Martal | Secondary |
| **Expansion win rate** (existing customers) | ~45% | Ebsta 2025 | Secondary |
| **B2B sales cycle (median)** | ~84–106 days. Mid-market ~6 months, enterprise 7–9 months | Gartner, Knowledgelib, others | Secondary |
| **Reps hitting quota** | 46% in 2025, down from 52% in 2024 | **Gong 2026** | **Primary** |
| | 48% of AEs (2026) vs 51% (2024) | Bridge Group 2026 | Secondary |
| | 78% of sellers missed quota | Ebsta × Pavilion 2025 | Secondary |
| **Revenue growth** (US orgs, avg) | 16% in 2025, down 3 points YoY. Sales velocity has plateaued | **Gong 2026** | **Primary** |
| **Time spent actually selling** | Less than half: "more than half of their time on nonselling work like data entry and prospecting" | **Salesforce 2026** | **Primary** |
| | B2B reps ~33% selling vs B2C/hybrid ~49% | SPOTIO | Secondary |
| **Revenue concentration** | Top 10% of reps = 65% of revenue. Bottom 50% = 7.6% | Clari 2025 (via **G2**) | Primary report citing secondary |
| **Customer acquisition cost** | Up 14% in 2024. Median $2 of S&M spend per $1 of new ARR | Benchmarkit 2025 (via **G2**) | Primary report citing secondary |
| **Cold email reply rate** | Most common range 1–4%. Only 16% exceed 5%. 69% say performance fell YoY | **Mailshake 2025** | **Primary** |
| **Cold email open rate** | Most 10–30%. Few above 40% | **Mailshake 2025** | **Primary** |

---

## 2. Funnel conversion (stage-by-stage)

No single authoritative benchmark exists. Vendor figures conflict because stage definitions differ.

| Stage | B2B SaaS (First Page Sage, via Prospeo) | Generic averages (Saber) |
|---|---|---|
| Lead → MQL | ~39% | ~13% |
| MQL → SQL | ~38% | ~31% |
| SQL → Opportunity | ~42% | ~22% |
| Opportunity/SQL → Closed-won | ~37% | — |

Overall funnel conversion is commonly quoted at **~2–10%**, depending on industry and funnel scope. Longer, complex sales (enterprise software, healthcare) have lower early-stage and higher late-stage rates.

---

## 3. AI and sales performance (primary data)

| Finding | Source |
|---|---|
| 87% of US orgs use AI in revenue teams. AI adoption grew 81% in 2025. ≥92% in every surveyed country expect to use it in 2026 | Gong 2026 |
| Orgs with AI as a **core strategy** reported **31% higher revenue growth** than those running limited pilots, and were 65% more likely to increase win rates | Gong 2026 |
| Revenue-specific AI correlates with 13% higher revenue growth | Gong 2026 |
| Sellers who use AI frequently generate **77% more revenue** than non-users (correlation, not proof of cause) | Gong 2026 |
| 43% expect AI to transform jobs **without** cutting headcount. 28% expect eliminations. 21% expect AI to create jobs | Gong 2026 |
| Top agent use cases: **fulfilling orders, tracking product usage (for usage pricing), creating quotes** | Salesforce 2026 |
| 46% of sales pros with agents say **data quality** issues hurt sales. Data leaders estimate **19% of their data is inaccessible** | Salesforce 2026 |
| 51% of sales leaders with AI say tech silos delay their initiatives. 42% of reps feel overwhelmed by too many tools | Salesforce 2026 |
| **Usage pricing is the #1 revenue model contributing to growth.** 76% of leaders say it matters more to customers than last year | Salesforce 2026 |
| 60% of 1,000+ B2B software buyers' companies use AI in sales | G2 2025 |
| Buyers want rep contact **later** in the journey: preferred contact at Evaluation rose 29% → 35% and at Decision 17% → 27% (2024 → 2025). 62% prefer contact at these later stages | G2 2025 |

---

## 4. Market size

| Market | Estimate | Note |
|---|---|---|
| Global B2B e-commerce 2026 | **$3.8T to ~$37T** | Huge spread. Web-only vs all electronic commerce (incl. EDI) vs GMV |
| US B2B e-commerce | ~$9.7T (2024) to ~$10.1T (2026). **~87% of all US e-commerce** | Experro, Grand View (via Blue Bolt) |
| B2B vs B2C | B2B is **4–5×** the size of B2C e-commerce | Multiple |

---

## 5. Order-to-cash KPIs (for the ERP side)

Public benchmarks are scarce (most are behind paid reports such as APQC/Opsdog), so these are **definitions plus the one hard number found**:

| KPI | Definition | Benchmark |
|---|---|---|
| **Fill rate** | Share of ordered units shipped from stock (980/1,000 = 98%) | Commonly targeted ≥95% (directional) |
| **Perfect order rate** | On-time × complete × damage-free × correct documents | Example ≈95.6% |
| **Quote → order conversion** | Orders ÷ quotes issued. Track lost-quote reasons too | No public benchmark found |
| **Back-order rate** | Lines not fully allocated at order entry ÷ total lines | No public benchmark found |
| **DSO** (days sales outstanding) | Receivables ÷ revenue × 365 (some use COGS, so check the basis) | — |
| **Cash-to-cash cycle** | DIO + DSO − DPO | **Median 50 days** (APQC, 15,617 orgs, cross-industry) |

---

## 6. Design implications for the Sales MCP

1. **Realistic seed data (M1):** generate ~20% quote win rates for new customers and ~45% for existing ones, cycles of 30–120 days by deal size, ~95–98% fill rate with some back-orders, and Pareto-shaped revenue (a few customers and reps dominate).
2. **Analytics (M7):** expose win rate (new vs expansion), cycle length, quote→order conversion, fill rate, back-order ageing and DSO in `reports://sales-summary/{period}`. All are defined in this doc, so the course can explain them.
3. **Usage pricing:** Salesforce's #1 growth model supports the stretch goal of **usage/subscription order lines**. Consider promoting it into M4/M7.
4. **Data quality is the main AI blocker,** so validation and a single source of truth in the domain layer are the core of the project, not polish.
5. **Buyers want reps later:** the MCP covers research and self-service, and `request_human_review` hands off at evaluation and decision.

---

## Sources
**Primary (local PDFs, links in [ai-in-sales-2023-2026.md](ai-in-sales-2023-2026.md#5-free-reports-downloadable-pdfs)):** Salesforce *State of Sales* 7th ed. (2026) · Gong Labs *State of Revenue AI 2026* · G2 *AI's Impact on Sales* (2025) · Mailshake *State of Cold Email 2025*.

**Secondary:**
- [Boomerang: quota attainment benchmarks 2026](https://www.getboomerang.ai/glossaries/quota-attainment-benchmarks-2026)
- [Konabayev: B2B sales benchmarks 2026](https://konabayev.com/blog/b2b-sales-benchmarks/)
- [Knowledgelib: sales metrics benchmarks 2026](https://knowledgelib.io/business/sales-ops/sales-metrics-benchmarks/2026)
- [Martal: B2B sales KPIs](https://martal.ca/sales-kpis-lb/)
- [Searchlab: sales statistics 2026](https://searchlab.nl/en/statistics/sales-statistics-2026)
- [Sendro: B2B sales in 2026](https://sendro.ai/blog/is-b2b-sales-difficult/)
- [Bebop: sourced B2B benchmarks](https://bebop.ai/statistics/sales-statistics)
- [SPOTIO: 150+ sales statistics](https://spotio.com/blog/sales-statistics/)
- [SPOTIO: state of field sales 2026](https://spotio.com/blog/state-of-field-sales-2026/)
- [Prospeo: sales funnel benchmarks](https://prospeo.io/s/sales-funnel-best-practices)
- [Saber: stage conversion benchmarking](https://www.saber.app/glossary/stage-conversion-benchmarking)
- [Lunas: real B2B conversion benchmarks](https://lunas.consulting/post/real-b2b-sales-conversion-rate-benchmarks-and-what-high-performing-teams-achieve-in-2026)
- [Elogic: B2B e-commerce statistics](https://elogic.co/blog/b2b-ecommerce-statistics/)
- [Experro: B2B e-commerce statistics 2026](https://experro.com/blog/b2b-ecommerce-statistics)
- [Blue Bolt: B2B vs DTC 2026](https://www.blueboltsolutions.com/b2b-ecommerce-vs-dtc-2026-data/)
- [Persistence Market Research: B2B e-commerce market](https://www.persistencemarketresearch.com/market-research/b2b-e-commerce-market.asp)
- [ExecViva: distribution KPIs](https://execviva.com/executive-hub/distribution-kpis)
- [Amazon Business: supply chain KPIs](https://business.amazon.com/en/blog/supply-chain-kpis)
- [Tecsys: supply chain metrics for distributors](https://tecsys.com/blog/2021/11/4-key-supply-chain-metrics-for-distributors)
- [Elogic: manufacturing & distribution KPIs 2026 (APQC figure)](https://elogic.co/?p=476632)
- [NextW: distribution metrics cheat sheet](https://resources.nextw.com/hubfs/Distribution_Metrics_Cheat_Sheet%20(2).pdf?hsLang=en)
