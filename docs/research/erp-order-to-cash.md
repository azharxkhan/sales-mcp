# How ERPs Model Order-to-Cash

> Research for [#13](https://github.com/azharxkhan/sales-mcp/issues/13). Researched 2026-11-11.
> Compares how the major ERPs model quotes, orders, pricing, credit, allocation and back-orders, to shape the Sales MCP domain (M1) and business rules (M4). **In depth (§2):** SYSPRO, SAP S/4HANA (SD), Microsoft Dynamics 365 Finance & Supply Chain, Odoo. **Also covered (§2b):** the Sage family (Evolution/200, Intacct, X3, 50/Pastel), SAP Business One, Oracle NetSuite, Dynamics 365 Business Central, Acumatica, Epicor Kinetic, Oracle JD Edwards and Infor. Vendor docs and training material vary by version. Items marked *(bg)* come from general background knowledge, so verify them against your version.

---

## 1. The common order-to-cash flow

Every ERP follows roughly the same chain, with each document **copied from** the one before it:

```
Inquiry → Quotation → Sales Order → (availability/credit checks) → Delivery / Pick-Pack-Ship → Invoice → Payment / Receipt
                         │
                         └─ Back order (unallocated quantity) → released when stock arrives
```

- **Document flow / traceability:** SAP links each document to its predecessor ("document flow") so you can trace inquiry → payment. Copy control decides which data carries forward.
- **Billing follows delivery:** in SAP the invoice is created from the delivery, so it reflects quantity *shipped*, not quantity ordered. Odoo makes this a per-product choice (below).

---

## 2. System-by-system

### SYSPRO (Sales Orders module)
| Concept | How SYSPRO does it |
|---|---|
| **Order types** | User-defined order type codes classify sales. Types have different rules: e.g. credit notes can't go on back order, inter-branch transfers can |
| **Quotations** | Captured through order entry (e.g. Quick Sales Order Entry) and converted to orders. Conversion needs a valid salesperson on the record |
| **Back orders** | Configured **per customer**: allow back orders or not, and allow **partial shipment** or ship only when complete. Setup options include automatic back-ordering |
| **Credit checking** | An extensive, tailorable credit and terms check. It can check limits on current orders only, and can postpone the check until an invoice is about to be produced. Orders that fail stay at their status (not moved to suspense) |
| **Order statuses** *(bg)* | Numbered statuses, e.g. 1 open, 2 open back order, 3 released back order, 4 in warehouse, 8 to invoice, 9 complete, plus S (suspense) and \* (cancelled). Check against your version |
| **Rules engine** | Newer versions market a rules engine that holds draft orders for validation before release |

### SAP S/4HANA Sales (SD)
| Concept | How SAP does it |
|---|---|
| **Documents** | Inquiry → Quotation (with validity period) → Sales Order → Outbound Delivery → Billing Document, all linked by document flow |
| **Availability** | **ATP (available-to-promise) check** at order entry: can the quantity be delivered on the requested date? A partial or later confirmation produces confirmed schedule lines |
| **Pricing** | **Condition technique:** prices, discounts, surcharges and taxes come from *condition records* found through an *access sequence* (customer + material, price list, material only, …) and summed by a *pricing procedure* |
| **Credit** | Credit exposure is compared with the authorised limit. Checks can trigger at order save, delivery creation or goods issue, depending on the customer's risk category. S/4HANA moves credit into FSCM Credit Management (credit master on the business partner) |
| **Blocks** | Orders can carry delivery/billing blocks (credit, pricing incomplete, etc.) until released |

### Microsoft Dynamics 365 (Supply Chain / Finance)
| Concept | How Dynamics does it |
|---|---|
| **Credit limits** | Turned on in *Accounts receivable parameters* ("Check credit limit on sales order"). Remaining credit can be calculated several ways (balance only, balance + packing slips, balance + all open orders) |
| **Credit holds** | Rules automatically **place orders on hold** when criteria are met (over limit, overdue balance, etc.). A held order can't be processed until someone releases it |
| **Reservation** *(bg)* | Inventory is reserved manually or automatically on order lines. Unreserved quantity remains open/back-ordered |

### Odoo
| Concept | How Odoo does it |
|---|---|
| **Quotation → order** *(bg)* | One record moves through states: *Quotation → Quotation Sent → Sales Order* (confirmed) → *Locked/Done*, or *Cancelled*. Confirming creates the delivery |
| **Invoicing policy** *(bg)* | Per product: invoice **ordered quantities** (invoice on confirmation) or **delivered quantities** (invoice what has shipped) |
| **Back orders** *(bg)* | When a delivery is validated with less than the full quantity, Odoo asks whether to create a **backorder** for the rest |
| **Credit limits** | Basic credit limits/warnings exist in recent versions. Many teams add community modules that **block order confirmation** when credit used + order total > limit, offer overrides, or add an "On Hold" state. Behaviour differs by module and version |

---

## 2b. Other important ERPs

### Why these matter
- **Sage and SYSPRO were born in South Africa** *(bg)*: Pastel (now Sage 50/Pastel) and Sage Evolution (Sage 200 Evolution) are among the most common SME/mid-market systems in SA, and SYSPRO was founded in Johannesburg. Any local B2B ordering tool will meet them.
- **SAP Business One, NetSuite, Business Central and Acumatica** dominate the global SME and mid-market cloud ERP space. **Epicor, JD Edwards and Infor** are big in manufacturing and distribution.
- No reliable SA installed-base market share data was found. One website-detection index lists SAP, Dynamics 365 and Odoo as most visible on SA websites, which says little about back-office ERP use.

### Sage family
| Product | Order-to-cash behaviour |
|---|---|
| **Sage 200 Evolution** (SA) *(bg)* | Quotations → Sales Orders → invoices. Back orders are processed with a dedicated back-order release step. Credit limit checks at order/invoice entry. Agent (user) permissions decide whether a user may exceed a credit limit or process accounts on hold. Third-party add-ons calculate "available credit" as limit − balance − open orders, which suggests native checks focus on balance |
| **Sage 200 (UK)** | Full order entry supports back-to-back purchase orders (needs a promised date). Stock is allocated from a default warehouse per user |
| **Sage Intacct** | Customer credit limits plus reports of over-limit/on-hold customers. Sales order transaction definitions can **warn** when an order will exceed the limit (users can still complete it) or **enforce** the limit |
| **Sage X3** (Enterprise Management) | Credit control with "limit exceeded" holds and manual hold reason codes. A parameter controls whether back-to-back orders may be created when credit is exceeded. Community reports show hold behaviour on order revisions can be inconsistent |
| **Sage 50 / Pastel Partner** (SA) *(bg)* | Quotes → sales orders → tax invoices, with credit limit warnings. Lighter allocation and back-order handling, typical of accounting-first systems |

### Mid-market and cloud ERPs
| Product | Quote → order | Credit control | Allocation and back orders |
|---|---|---|---|
| **SAP Business One** *(bg)* | Sales Quotation → Sales Order → Delivery → A/R Invoice ("copy to"/"copy from") | Credit limit and commitment limit checks. **Approval procedures** can stop documents for authorisation | Available-to-promise from in stock − committed + ordered. Partial deliveries leave open quantity |
| **Oracle NetSuite** | Estimate → Sales Order. Optional **approval** step (Pending Approval → Pending Fulfillment) | Credit limits with warn or block. **Credit holds** (often via workflow) stop approval until lifted | **Commit** inventory per line (commit options include "do not commit"). Uncommitted quantity shows as **backordered** at line level. A *hold* is a separate order-level flag |
| **Dynamics 365 Business Central** | Sales Quote → "Make Order" | Checks on quotes, orders and invoices. Warns on credit limit, overdue balance, both or none. **Native behaviour warns but doesn't block.** Blocking needs approval workflows or extensions. Quotes don't count until converted | Availability warning while keying lines. Reservations optional |
| **Acumatica** | Quote (QT) → Sales Order (SO) order types | Order types can **hold on failed credit check** (Credit Hold status). Hold can auto-remove when payment brings the customer under limit. A customer on credit hold blocks release | Auto-allocate per order type. Allocation can happen even on held orders (a known concern: stock tied up on held orders) |
| **Epicor Kinetic** | Quote → Sales Order | By default, orders exceeding limits go **on hold until cleared by a credit controller**. No picking or shipping until released. Customer-level credit hold and order-level hold are separate flags | Order release/fulfilment workbench *(bg)* |
| **Oracle JD Edwards** | Sales order entry with order types | **Hold codes** (credit, margin, partial, etc.) applied automatically at entry and released through a hold-release program by authorised users | Commitments (hard/soft) and backorder handling per branch and item *(bg)* |
| **Infor CloudSuite** | Order to cash covered in docs (order, billing invoice, payment) | No detailed public credit-hold docs found | Not researched (gap) |

### The pattern across all of them

Credit control falls into **three styles**:

| Style | Who uses it | Trade-off |
|---|---|---|
| **Warn only** | Business Central (native), Sage Intacct (warn mode), Sage 50/Pastel | Fast, but relies on user discipline |
| **Hold → release by an authorised person** | SAP (credit block), Dynamics 365 F&SCM, NetSuite, Acumatica, Epicor, JD Edwards, Sage X3, SYSPRO (order stays at status) | The **majority pattern** in serious B2B ERPs. Auditable, and keeps a human in the loop |
| **Block at confirmation** | Odoo (via modules), Sage Intacct (enforce mode) | Strict, but frustrating without an override path |

**Allocation vs holds:** NetSuite and Acumatica both show the same subtlety. *Back-ordered* (no stock) is a line-level state, and *on hold* (credit/approval) is an order-level state. They must be modelled separately.

---

## 3. Comparison

| Capability | SYSPRO | SAP S/4HANA | Dynamics 365 | Odoo |
|---|---|---|---|---|
| Quote as its own document | Converted into an order | ✅ Separate document with validity | ✅ (sales quotation) *(bg)* | Same record, different state |
| Availability check | Stock allocation at entry | ✅ ATP with dates | Reservation | Forecasted quantity at confirmation |
| Back-order control | **Per customer**, partial-ship flag | Partial/complete delivery per customer *(bg)* | Per order/line *(bg)* | Backorder prompt at delivery |
| Pricing engine | Price codes / contract pricing *(bg)* | **Condition technique** (most flexible) | Trade agreements *(bg)* | Pricelists with rules |
| Credit check timing | Order or **before invoice** (configurable) | Order / delivery / goods issue | Order (rules → hold) | Confirmation or invoice (module-dependent) |
| Credit failure result | Order stays at status | Credit block | **Hold** until released | Blocked / override / on hold |
| Billing basis | Shipped quantity *(bg)* | Delivery | Packing slip / invoice | **Per product**: ordered or delivered |

---

## 4. Design decisions for the Sales MCP domain

These turn the comparison into concrete M1/M4 choices (candidate ADRs):

| Decision | Choice for Sales MCP | Modelled on |
|---|---|---|
| **Quote vs order** | Separate `Quote` and `SalesOrder` aggregates. `convert_quote_to_order` copies lines (document-flow link kept as `SourceQuoteId`) | SAP document flow, SYSPRO conversion |
| **Quote validity** | `ValidUntil` (default 30 days). Expired quotes can't be converted | SAP quotation validity |
| **Order status** | `Draft → Released → PartiallyShipped → Shipped → Invoiced`, plus `OnHold` and `Cancelled`. Display-mapped to SYSPRO-style codes in docs for familiarity | SYSPRO statuses + Dynamics holds |
| **Credit check** | Run at **order release** (configurable: `OnOrder` or `BeforeInvoice`). Failure sets `OnHold` with a reason. Release needs `request_human_review`. This is the **hold → release** style used by most B2B ERPs (§2b), plus an optional `WarnOnly` mode to mirror Business Central/Pastel | SYSPRO configurable timing, Dynamics/NetSuite/Acumatica/Epicor holds |
| **Hold vs back order** | Separate concepts: `OrderHold` (credit/approval, order-level, with reason codes as in JD Edwards) vs `BackOrderLine` (stock, line-level). An order can be both | NetSuite, Acumatica, JD Edwards |
| **Allocation on held orders** | Configurable `AllocateWhileOnHold` (default **false**), to avoid tying up stock on orders that may never ship | Acumatica's known pain point |
| **Quotes and credit** | Quotes **don't** count toward credit exposure. Only released orders do | Business Central behaviour |
| **Credit exposure formula** | `Balance + OpenOrdersValue + ThisOrder > CreditLimit` (configurable to balance-only) | Dynamics remaining-credit options |
| **Availability** | Allocate from `OnHand − Allocated` per warehouse at release. Report `AvailableDate` from `OnOrder` (purchase orders) for a simple ATP | SAP ATP (simplified) |
| **Back orders** | Customer flags `AllowBackOrders` and `AllowPartialShipment`. A shortfall becomes back-order lines. Credit notes can never back-order | **SYSPRO per-customer settings** |
| **Pricing** | Small condition-style resolver: contract price (customer + SKU) → customer price list → quantity break → list price, then discount, then VAT. Each step recorded in a `PriceBreakdown` the tool returns | SAP condition technique (simplified), Odoo pricelists |
| **Billing basis** | Invoice **shipped** quantity (M4+). Support `InvoicePolicy = Ordered` on service items | SAP billing from delivery, Odoo invoicing policy |
| **Order types** | `Standard`, `CreditNote`, `Transfer` (later). Each type carries its own rules | SYSPRO order types |
| **Auditability** | Every state change records an `AuditEntry` and the source document link | SAP document flow |

**Why it matters for the course:** each rule is a real ERP concept, so learners see *why* the code exists. The `PriceBreakdown` and credit-hold reasons give explainable AI output, which addresses the trust problems in [fixing-ai-selling.md](fixing-ai-selling.md).

---

## Sources
- [SYSPRO: Sales Order Entry (SYSPRO 8 2025 help)](https://help.syspro.com/syspro-8-2025/g_programs/imp/imp040/imp040.htm)
- [SYSPRO: Sales Orders overview (SYSPRO 7 help)](https://help.syspro.com/syspro-7-update-1/_sorovr.htm)
- [SYSPRO: Sales Order Setup (SYSPRO 7 help)](https://help.syspro.com/syspro-7-update-1/impord.htm)
- [SYSPRO: Sales Order Query](https://help.syspro.com/syspro-8-2025/g_programs/sor/sorpen/sorpen.htm)
- [SYSPRO: Sales Order Management product page](https://www.syspro.com/product/sales-order-management/)
- [SYSPRO: Sales Orders fact sheet (PDF)](https://usuat.syspro.com/dl/FS/SYSPRO-Sales-Orders-FS.pdf)
- [SAP Learning: executing an order-to-cash process in S/4HANA Sales](https://learning.sap.com/learning-journeys/applying-sap-s-4hana-sales/executing-an-order-to-cash-process-in-sap-s-4hana-sales_c2c3bf8e-d8d4-3022-b8aa-6ca259e8b9b0)
- [SAP PRESS: S/4HANA sales architecture overview](https://blog.sap-press.com/sap-s4hana-sales-architecture-overview)
- [HPI: Sales and Distribution lecture (PDF)](https://hpi.de/oldsite/fileadmin/user_upload/fachgebiete/plattner/teaching/TrendsandConcepts/2022/04_Sales_and_Distribution_20220511.pdf)
- [SAP SD end-to-end sales cycle](https://mohanranga.squarespace.com/blogs/sd-end-to-end-sales-cycle)
- [Microsoft Learn: credit limits for customers (Supply Chain Management)](https://learn.microsoft.com/en-us/dynamics365/supply-chain/sales-marketing/credit-limits-customers)
- [Microsoft Learn: manage customer credit limits (Finance)](https://learn.microsoft.com/pt-br/dynamics365/guidance/business-processes/order-to-cash-monitor-customer-credit-collections-set-customer-credit-limits)
- [Microsoft Learn: sales order processing in Dynamics GP](https://learn.microsoft.com/en-us/dynamics-gp/distribution/sales-order-processing)
- [Odoo Apps: sale credit limit module (example of the block-on-confirm pattern)](https://apps.odoo.com/apps/modules/19.0/sh_sale_credit_limit)
- [Odoo Apps: credit limit risk hold module](https://apps.odoo.com/apps/modules/19.0/gb_credit_limit_risk_hold)
- [Sage 200: enter full sales orders](https://desktophelp.sage.co.uk/sage200/sage200standard/Content/Concepts/SOP_orders_Full.htm)
- [Sage 200: enter the sales order details](https://desktophelp.sage.co.uk/sage200/professional/Content/SOP/Enter_the_order_details.htm)
- [Sage Intacct: credit limits for customers](https://www.intacct.com/ia/docs/en_AU/help_action/Accounts_Receivable/Setup/Credit_limits/credit-limits-for-customers.htm)
- [Sage X3 community: order not going on credit hold](https://communityhub.sage.com/sage-global-solutions/sage_x3/f/general-discussion/234243/sales-order-not-going-on-credit-hold-when-customer-exceeds-credit-limit)
- [Greytrix: back-to-back orders when credit is exceeded (Sage X3)](https://www.greytrix.com/blogs/sagex3/?p=16728)
- [SWK: Sage X3 customer hold and order release](https://swktech.com/sage-erp-x3-customer-hold-order-held-release-order)
- [Paradigm: customer credit control with Sage Evolution (add-on)](https://softwareandhosting.freshdesk.com/support/solutions/articles/43000360739-customer-credit-control)
- [Houseblend: NetSuite credit limits, holds and order release (PDF)](https://www.houseblend.io/articles/pdfs/netsuite-credit-limits-holds-order-release-controls.pdf)
- [BrokenRubik: NetSuite order management guide](https://www.brokenrubik.com/blog/netsuite-order-management-guide)
- [Oracle community: committing inventory on an order pending approval](https://community.oracle.com/Netsuite/english/discussion/4229357/committing-inventory-on-an-order-pending-approval)
- [Stoneridge: credit limit approval workflows in Business Central](https://stoneridgesoftware.com/managing-credit-limits-business-central-implementing-credit-limit-approval-process/)
- [Rand Group: credit limit locks in Business Central](https://www.randgroup.com/insights/microsoft/dynamics-365/business-central/how-to-set-up-credit-limit-locks-in-business-central/)
- [Sana Commerce: Business Central credit limits](https://support.sana-commerce.com/Content/ERP-User-Guide/D365BC/Customers/Credit-Limits-D365BC.htm)
- [MYOB Acumatica: credit verification](https://help.myob.com.au/advanced/Docs/Published/UserGuide/AR__CON_Credit_Verification.html)
- [MYOB Acumatica: changing the processing of sales documents](https://help.myob.com.au/advanced/Docs/Published/UserGuide/SO__con_AffectingProcessingDocs.html)
- [Acumatica community: order with stock allocation](https://community.acumatica.com/distribution-6/order-with-stock-allocation-19812)
- [Epicor: credit controls overview (course transcript, PDF)](https://ondemand.epicor.com/courseResources/r2049_BT122CreditControlsOverviewTranscript_1.pdf)
- [EpiUsers: sales order holds](https://www.epiusers.help/t/sales-order-holds/54838.md)
- [Oracle: JD Edwards, understanding holding orders](https://docs.oracle.com/en/applications/jd-edwards/supply-chain-manufacturing/9.2/eoaso/understanding-holding-orders.html)
- [Infor: order to cash (contextual applications)](https://docs.infor.com/ifsm/2025.x/en-us/ifsmolh/ifsmctxappsug/tqu1664175428863.html)
- [Wmtips: ERP systems detected on South African websites](https://www.wmtips.com/technologies/erp/country/za/)
