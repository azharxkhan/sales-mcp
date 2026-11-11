# How ERPs Model Order-to-Cash

> Research for [#13](https://github.com/azharxkhan/sales-mcp/issues/13). Researched 2026-11-11.
> Compares how SYSPRO, SAP S/4HANA (SD), Microsoft Dynamics 365 and Odoo model quotes, orders, pricing, credit, allocation and back-orders, to shape the Sales MCP domain (M1) and business rules (M4). Vendor docs and training material vary by version. Items marked *(bg)* come from general background knowledge, so verify them against your version.

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
| **Credit check** | Run at **order release** (configurable: `OnOrder` or `BeforeInvoice`). Failure sets `OnHold` with a reason. Release needs `request_human_review` | SYSPRO configurable timing, Dynamics hold/release |
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
