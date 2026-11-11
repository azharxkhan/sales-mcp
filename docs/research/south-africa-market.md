# South African Sales and B2B Market

> Research for [#14](https://github.com/azharxkhan/sales-mcp/issues/14). Researched 2026-11-11.
> The project defaults to **ZAR and 15% VAT** ([PLAN ADR-0007](../PLAN.md#2-key-decisions-each-becomes-an-adr-in-docsadr)). This doc gives local context and the rules that shape how sales data and AI selling must be handled. **Not legal advice.** Items marked *(bg)* come from general background knowledge, so verify them with the legislation or a lawyer before relying on them.

---

## 1. Market snapshot

| Metric | Value | Source |
|---|---|---|
| **Online retail 2026 (forecast)** | **R159bn**, +22.5% YoY | World Wide Worx / Mastercard / Peach Payments, *Online Retail in SA 2026* |
| Online retail 2025 | R130bn | Same |
| Online retail 2024 | R96bn, +35% YoY (≈8% of retail) | WWW 2025 report |
| **Online share of total retail** | 8.49% end-2025 → **passed 10% in mid-2026** | WWW / Stats SA |
| Overall retail growth | ~4% (first five months of 2026) | Same |
| Checkers Sixty60 | R25.5bn, +34.5% (year to June 2026) | Same |
| Amazon share of SA online retail | 12.7%, expected to rise after its Prime launch | Same |
| Shoppers using AI chat tools when shopping online | **41%** (SA ranks 6th of surveyed markets) | DHL Express (via OrderEazi) |

**B2B and wholesale picture** (mostly vendor commentary, so directional):
- Many SA wholesalers and distributors still take orders **manually** (phone, email, WhatsApp, reps), because generic e-commerce tools don't fit B2B needs such as account pricing, credit terms and back-orders.
- B2B payment runs largely on **EFT**, with local gateways (PayFast, Ozow, Peach Payments, SnapScan) for digital payments.
- 9 in 10 SMEs now accept digital payments, but adopting broader digital tools is still slow.
- Retailers are mostly **early** in AI adoption. Connectivity cost, data quality and customer trust are bigger constraints than model choice.

**Takeaway:** online is growing about 5× faster than retail overall, but B2B ordering is still largely manual. An "ERP-first" ordering assistant (what this MCP demonstrates) addresses a real local gap.

---

## 2. Rules that affect sales data and AI selling

### VAT (SARS)
| Item | Value |
|---|---|
| **Standard rate** | **15%**. The 2025 increases (15.5% then 16%) were **reversed** and the 2026 Budget left the rate unchanged |
| Compulsory VAT registration | Turnover above **R2.3m** (raised from R1m from 1 Apr 2026) |
| Voluntary registration | R120k (from R50k) |
| Zero-rated / exempt supplies *(bg)* | Some basic foods and exports are zero-rated. Some financial services are exempt. Needs per-item VAT codes |
| **E-invoicing** | **Not yet mandatory.** Definitions were added in 2026 law. Sources disagree on timing (a phased rollout aiming for ~2028 vs a B2B mandate "not before 2030"). Watch SARS |
| Tax invoice content *(bg)* | Full tax invoices need the supplier's and recipient's VAT numbers, a unique invoice number, date, description, value and VAT amount |

### POPIA (Protection of Personal Information Act)
| Rule | What it means here |
|---|---|
| **s69: electronic direct marketing** | Email, SMS, automated calls (and, per the Regulator, phone calls) need **opt-in consent** in the prescribed form, **or** an existing-customer relationship (similar products, with an opt-out offered each time). **Opt-out does not count as consent.** Every message must identify the sender and include an opt-out |
| Information Regulator Guidance Note (Dec 2024) | Confirms the s69 approach and active enforcement against unsolicited marketing |
| **s71: automated decision-making** *(bg)* | People may not be subject to decisions with legal or similarly significant effects based **solely** on automated processing (e.g. credit decisions) without safeguards such as the chance to make representations |
| Conditions for lawful processing *(bg)* | Purpose limitation, minimality, security safeguards, data subject rights (access, correction, deletion) and breach notification |
| AI chatbots | Commentary is unsettled. One academic paper argues chatbots fall outside s69. The Regulator focuses on the channel, not the technology. Treat AI outreach as direct marketing |

### Consumer Protection Act (CPA) and ECTA
| Rule | What it means here |
|---|---|
| CPA: direct marketing *(bg)* | Right to refuse or block direct marketing (s11). **5-business-day cooling-off** for sales resulting from direct marketing (s16). 2026 amendment regulations noted by the Information Regulator |
| ECTA: online sales *(bg)* | s43 requires online sellers to disclose identity, price, terms and return policy. s44 gives consumers a **7-day cooling-off** period for many electronic transactions |
| ECTA s45 | The old spam opt-out rule was **repealed** and replaced by POPIA s69 |
| Scope note *(bg)* | The CPA mostly protects consumers and smaller juristic persons. Large B2B buyers are largely outside it, but POPIA applies to *personal information* of B2B contacts too |

---

## 3. Design implications for the Sales MCP

| Finding | Design choice |
|---|---|
| ZAR, 15% VAT, zero-rated items | `Money` value object in ZAR. **Per-item `VatCode`** (Standard 15%, Zero, Exempt) instead of one flat rate. Rate configurable for future changes |
| Tax invoice requirements | Customer has `VatNumber`. Documents carry unique numbers and VAT breakdowns. Ready for SARS e-invoicing later (structured invoice data) |
| **POPIA s69** | **No outbound marketing tools.** Follow-ups only to existing customers about their own orders/quotes. Store `MarketingConsent` (opt-in, timestamp, channel) on contacts if a contact feature is ever added |
| **POPIA s71** (automated decisions) | Credit holds are **never final when made by AI alone**. `OnHold` plus `request_human_review` gives a human decision and a chance for representations. This aligns the guardrail design with local law |
| POPIA minimality and security | Seed data uses **synthetic** people and companies only. Tools return only the fields needed. Audit log of access. No personal information in logs |
| CPA/ECTA disclosure and cooling-off | Quote output includes price breakdown, VAT, validity and terms. Document `ReturnPolicy` / cooling-off as a policy resource (`policy://returns`) |
| Manual B2B ordering is common locally | README/course story: "SA distributor taking WhatsApp and phone orders → MCP-powered self-service with ERP rules" |
| 41% of SA shoppers use AI chat tools | Supports the self-service channel thesis locally, not just in the US |

---

## Sources
- [ITWeb: SA online retail races to R159bn](https://www.itweb.co.za/article/sa-online-retail-races-to-r159bn-as-growth-accelerates/o1Jr5MxPopQMKdWL)
- [Engineering News: online is 10% of SA retail](https://engineeringnews.co.za/article/online-constitutes-10-of-all-retail-sales-in-south-africa-2026-09-02)
- [TechCentral: online retail in SA 2026](https://techcentral.co.za/online-retail-south-africa-2026-world-wide-worx/285678/)
- [Bizcommunity: online retail reaches 10% as sales hit R159bn](https://www.bizcommunity.com/article/online-retail-reaches-10-of-south-africa-market-as-sales-hit-r159bn-419199a)
- [Peach Payments: 2026 Online Retail Report](https://www.peachpayments.com/2026-online-retail-report/)
- [TechCabal: SA online retail boom](https://techcabal.com/2025/09/11/south-africas-online-retail-boom-exceeds-7bn)
- [OrderEazi: why SA B2B businesses are moving to online ordering](https://ordereazi.com/content?id=106) (vendor)
- [Bizcommunity: digital dithering holds back SA small businesses](https://www.bizcommunity.com/article/digital-dithering-holds-back-sa-small-businesses-287447a)
- [Engineering News / Accenture: be unmissable to AI](https://www.engineeringnews.co.za/article/the-next-shift-in-commerce-why-sa-businesses-must-become-unmissable-to-ai-2026-05-25)
- [Schneider Electric: e-commerce is a necessary evolution in SA](https://se.com/za/en/about-us/newsroom/news/press-releases/e-commerce-is-a-necessary-evolution-for-business-growth-in-south-africa-67580002ac404dd2a90c12dd)
- [SARS: Value-Added Tax](https://www.sars.gov.za/tax-types/value-added-tax/)
- [Tax Consulting SA: Budget 2026 and VAT](https://www.taxconsulting.co.za/budget-2026-vat-the-quiet-revenue-lever-sars-is-already-pulling/)
- [Avalara: VAT hike cancelled, e-invoicing planned](https://www.avalara.com/blog/en/europe/2025/05/south-africa-e-invoicing-vat-update-2025.html)
- [LookupTax: e-invoicing in South Africa](https://lookuptax.com/docs/einvoicing/digital-vat-model-einvoicing-south-africa)
- [BusinessTech: big VAT changes coming](https://businesstech.co.za/news/finance/849589/big-vat-changes-coming-for-south-africa-2/)
- [MJK Inc: POPIA electronic marketing (s69)](https://mjkinc.co.za/popia/electronic-marketing-section-69)
- [Baker McKenzie: direct marketing under scrutiny (2024)](https://insightplus.bakermckenzie.com/bm/data-technology/south-africa-direct-marketing-under-scrutiny-key-regulatory-developments-in-2024)
- [DLA Piper: Guidance Note on direct marketing (2025)](https://www.dlapiperafrica.com/en/south-africa/insights/2025/Data-Protection-Guidance-Note-on-Direct-Marketing)
- [Fasken: more red tape for direct marketers? (2026)](https://www.fasken.com/en/knowledge/2026/09/a-step-in-the-right-direction-or-more-red-tape-for-direct-marketers)
- [Information Regulator: CPA amendment regulations 2026 (media statement)](https://inforegulator.org.za/wp-content/uploads/2026/04/MEDIA-STATEMENT-REGULATOR-NOTES-PUBLISHED-GAZETTE-OF-THE-CONSUMER-PROTECT-ACT-AMENDMENT-REGULATIONS-2026.pdf)
- [Go Legal: direct marketing in South Africa](https://www.golegal.co.za/legislation-direct-marketing/)
- [Wits WIReDSpace: academic paper on POPIA direct marketing and AI](https://wiredspace.wits.ac.za/bitstreams/0b977808-10d5-49eb-86a0-061c64051f47/download)
