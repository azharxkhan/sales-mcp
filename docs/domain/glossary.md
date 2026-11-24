# Domain glossary

The sales and ERP terms this project uses, and what each means here. Most of them come from how ERPs model order-to-cash: see [erp-order-to-cash.md](../research/erp-order-to-cash.md) for the comparison, and [§4](../research/erp-order-to-cash.md#4-design-decisions-for-the-sales-mcp-domain) for the design decisions they led to.

**Status** says whether the concept is in the code yet. *M1* means it's built. A later milestone means the term is defined now so the course and the code use the same words from the start.

## Order-to-cash

| Term | Meaning in this project | Status | Code |
|---|---|---|---|
| **Quote** | A numbered offer to one customer, with priced lines, a validity date (30 days by default) and optional risk-reducers. It's the only commitment an agent can make besides an order, and only a tool can issue one ([ADR-0015](../adr/0015-commitments-only-through-tool-issued-documents.md)). A quote is issued, then accepted, declined or withdrawn. An expired quote can't be accepted | M1 | [`Quote`](../../src/SalesMcp.Domain/Quotes/Quote.cs) |
| **Sales order** (order) | The customer's order. It exists only by converting an accepted quote, keeps a link to that quote and charges exactly what was quoted. In M1 an order moves `Draft → Released → Shipped → Invoiced`. `PartiallyShipped` and `Cancelled` exist as statuses but are used from M4 | M1 (skeleton) | [`SalesOrder`](../../src/SalesMcp.Domain/Orders/SalesOrder.cs) |
| **Hold** | A block that stops an order being released until a human resolves it. It has a reason: credit, discount approval or review. It applies to the **whole order**. A credit hold always goes to human review. M1 has a simpler customer-level `OnHold` flag | M4 | [`Customer.OnHold`](../../src/SalesMcp.Domain/Customers/Customer.cs) |
| **Back-order** | The part of an **order line** that can't ship now and ships when stock arrives. Only allowed when the customer's `AllowBackOrders` flag is on. It's not a hold: a hold is about the order (credit, approval), a back-order is about the stock on one line. An order can have both | M4 | [`Customer.AllowBackOrders`](../../src/SalesMcp.Domain/Customers/Customer.cs) |
| **Allocation** | Reserving available stock for an order line when the order is released, so two orders can't promise the same units. Allocated stock is no longer available to others. By default, nothing is allocated while an order is on hold | M4 | [`StockLevel.Allocated`](../../src/SalesMcp.Domain/Inventory/StockLevel.cs) |
| **Credit exposure** (exposure) | How much a customer would owe if this order went through: balance + open released orders + this order. If it's over the credit limit, the order goes on a credit hold. Quotes don't count | M4 | [`Customer.CreditLimit`, `Balance`](../../src/SalesMcp.Domain/Customers/Customer.cs) |
| **ATP** (available to promise) | When a quantity can ship. Today if it's in stock. The next receipt date if the stock on order covers it. Otherwise unknown, and the agent must say so instead of guessing | M1 | [`StockLevel.AvailableDate`](../../src/SalesMcp.Domain/Inventory/StockLevel.cs) |

## Catalog and pricing

| Term | Meaning in this project | Status | Code |
|---|---|---|---|
| **Offering** | Anything sold: a product, a service or a plan | M1 (products); M6 (services, plans) | [`Offering`](../../src/SalesMcp.Domain/Catalog/Offering.cs) |
| **Offering rule** | A link between two offerings: one *requires* the other, *excludes* it, or *recommends* it. Each rule has a reason the agent can give the buyer | M1 | [`OfferingRule`](../../src/SalesMcp.Domain/Catalog/OfferingRule.cs) |
| **Contract price** | A price agreed with one customer for one offering, valid for a date range. The first source the price resolver tries | M1 | [`ContractPrice`](../../src/SalesMcp.Domain/Pricing/ContractPrice.cs) |
| **Price list** | A set of prices. Each business has one default list. A customer may also have their own | M1 | [`PriceList`](../../src/SalesMcp.Domain/Pricing/PriceList.cs) |
| **Quantity break** | A lower unit price from a minimum quantity, for example ZAR 90 each from 10 units. The best break for the quantity applies | M1 | [`PriceList.BestBreak`](../../src/SalesMcp.Domain/Pricing/PriceList.cs) |
| **Price breakdown** | The record of how a price was reached: its source, unit price, gross, discount, VAT, total, the time it was priced, and one step per decision. It lets an agent explain a price, not just state it | M1 | [`PriceBreakdown`](../../src/SalesMcp.Domain/Pricing/PriceBreakdown.cs) |
| **Snapshot** | A copy of the price breakdown stored on a quote line. Later price or catalog changes don't affect it | M1 | [`QuoteLine`](../../src/SalesMcp.Domain/Quotes/QuoteLine.cs) |
| **VAT code** | *Standard* (15% in South Africa), *zero-rated* (0%, still reported) or *exempt* (no VAT at all). VAT is calculated and rounded per line ([ADR-0007](../adr/0007-locale-zar-and-vat-codes.md)) | M1 | [`VatCode`](../../src/SalesMcp.Domain/Tax/VatCode.cs) |
| **Risk-reducer** | Something that lowers the buyer's risk instead of the price: a trial, phased delivery, cancel anytime or a return window | M1 | [`RiskReducer`](../../src/SalesMcp.Domain/Quotes/Quote.cs) |

## Data and testing

| Term | Meaning in this project | Status | Code |
|---|---|---|---|
| **Seed manifest** | Named rows in the seed data, such as the top customer or a product with stock on order, that tests and evaluations refer to by name. Their ids are the same on every run | M1 | [`SeedManifest`](../../src/SalesMcp.Infrastructure/Seeding/SeedManifest.cs) |

Agent, playbook and guardrail terms join this page in M3 and M4, with the code that defines them.
