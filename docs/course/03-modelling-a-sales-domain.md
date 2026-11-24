# Chapter 3: Modelling a sales domain

> **Milestone:** M1 · **Code:** PRs [#51](https://github.com/azharxkhan/sales-mcp/pull/51) to [#59](https://github.com/azharxkhan/sales-mcp/pull/59) · **Next:** [Chapter 4: Persistence](04-persistence.md)

## What you'll build

A sales domain in plain C#, with no database, no MCP and no AI: offerings, customers, price lists, VAT, stock and quotes. At the end, a unit test can price 12 bearings for a trade customer, add VAT and issue a quote that stays at that price after the price list changes.

```text
12 × ACM-BRG-6204 for a trade customer, 12.5% off
  List 2026, 10+ break      ZAR   90.00 each
  Gross                     ZAR 1 080.00
  12.5% off                 ZAR  -135.00
  VAT 15%                   ZAR   141.75
  Total                     ZAR 1 086.75
```

## Why it matters

An AI sales agent is only as trustworthy as the facts it's given. The most common failure in AI selling is a confident, wrong answer about price, stock or terms ([fixing-ai-selling.md](../research/fixing-ai-selling.md)). Prompts can't prevent that, so the numbers must come from code that can't get them wrong ([ADR-0011](../adr/0011-guardrails-in-tools-not-prompts.md)).

That code already exists in every ERP. SYSPRO, SAP, Dynamics and Odoo all separate quotes from orders, resolve prices in a fixed order and treat credit holds and back-orders as different things ([erp-order-to-cash.md](../research/erp-order-to-cash.md)). This chapter copies those ideas, kept small, so the MCP tools in later chapters sit on a real engine ([ADR-0003](../adr/0003-b2b-selling-on-erp-grade-engine.md)).

## Concepts

**The Domain project depends on nothing.** `SalesMcp.Domain` has no NuGet packages and no references to other projects. Tests enforce this ([ADR-0004](../adr/0004-clean-layered-architecture.md)), so the business rules can be read, run and tested on their own. Words in bold below are defined in the [glossary](../domain/glossary.md).

**Value objects, not primitives.** A price is a `Money`, not a `decimal`. A customer id is a `CustomerId`, not a `Guid`. The compiler then catches mistakes like passing an offering id where a customer id was expected, or adding rands to dollars.

**Results, not exceptions, for business rules.** A customer who asks for an item that has no price isn't a bug, it's an answer. Rule failures return `Result<T>` with a `DomainError(Code, Message)`. Exceptions are kept for bugs, such as a null argument. In chapter 5 an MCP tool turns that error into a message the model can read and explain.

**Aggregates own their lines.** A `Quote` owns its `QuoteLine`s. Lines are created only through `Quote.Create`, and totals are always computed from the lines, never stored separately and trusted.

**No hidden clocks or dice.** `DateTime.Now`, `DateTimeOffset.UtcNow` and `new Random()` are banned in Domain and Application ([src/BannedSymbols.txt](../../src/BannedSymbols.txt)), and using them fails the build. Time is passed in, so every rule can be tested at any date.

## Step by step

Each step was one PR, written tests first. Run the tests after each one:

```bash
dotnet test --solution SalesMcp.slnx
```

### Step 1: building blocks

[`src/SalesMcp.Domain/Common/`](../../src/SalesMcp.Domain/Common/) holds the types everything else uses:

| Type | Rule it enforces |
|---|---|
| `Money` | An amount and an ISO 4217 currency. Adding two currencies throws. Rounding is explicit: `Round()` goes to 2 decimals, midpoint away from zero |
| `Quantity` | Always greater than zero, up to 4 decimals (metres, litres, kilograms) |
| `Percentage` | Between 0 and 100 |
| `BusinessId`, `OfferingId`, `CustomerId`, … | A `Guid` wrapped in its own type. New ids are version 7 Guids, which are time-ordered and index well |
| `Result`, `Result<T>`, `DomainError` | Success or a coded failure |

Each value object has two ways in. The constructor throws on bad input, because passing bad input from code is a bug. `Create` returns a `Result`, for input that comes from a user or a tool call:

```csharp
public static Result<Quantity> Create(decimal value) =>
    value > 0m
        ? Result.Success(new Quantity(value))
        : Result.Failure<Quantity>(NotPositiveCode, "The quantity must be greater than zero.");
```

Money keeps full precision until you round it. `MoneyTests.Keeps_full_precision_until_rounded` shows why: rounding each step early adds up to cents of drift on a long quote.

### Step 2: VAT

South Africa has three VAT treatments: standard (15%), zero-rated (0%, for example basic foods) and exempt (no VAT at all, for example some financial services). Zero-rated and exempt both charge no VAT, but they are reported differently, so they are different codes ([ADR-0007](../adr/0007-locale-zar-and-vat-codes.md)).

`VatRateTable.Calculate` rounds the net amount, works out the VAT and rounds that too, **per line**:

```csharp
public VatLine Calculate(Money net, VatCode code)
{
    var roundedNet = net.Round();
    var rate = RateFor(code);
    var vat = rate is { } r ? r.Of(roundedNet).Round() : Money.Zero(net.Currency);
    return new VatLine(code, rate, roundedNet, vat, roundedNet + vat);
}
```

`VatSummary` adds the lines up by code for the quote total. Because each line is already rounded, the total is exactly the sum of the lines a buyer sees.

### Step 3: offerings and rules

An **offering** is anything sold. `Offering` is an abstract class with three kinds: `Product` (built in M1), and `Service` and `Plan` (stubs until M6). They share an id, a SKU, a name, a description and a VAT code, and can be deactivated without being deleted.

`OfferingRule` links two offerings:

| Kind | Meaning | Acme example |
|---|---|---|
| `Requires` | Can't sell A without B | A cordless drill body needs an 18 V battery |
| `Excludes` | Can't sell A with B (works both ways) | A 230 mm disc doesn't fit a 115 mm grinder |
| `Recommends` | Suggest B with A | Eye protection with a grinder |

`OfferingRuleSet.Add` refuses a duplicate and refuses a rule that contradicts an existing one, such as "A requires B" after "A excludes B". Every rule carries a reason, because the agent has to explain it to a buyer.

### Step 4: customers and contacts

A B2B `Customer` is an account, not a person ([ADR-0014](../adr/0014-b2b-first.md)). It holds the fields ERPs use to decide whether and how to sell:

- `CreditLimit` and `Balance`, used for **credit exposure** in M4
- `OnHold`, which stops new orders until a human releases it
- `PaymentTerms`, such as 30 days from the end of the month
- `PriceListId`, the customer's own price list, if any
- `AllowBackOrders` and `AllowPartialShipment`, which decide what happens when stock runs short

A `Contact` is a person at the customer. Marketing consent is stored with when and where it was given, because POPIA requires proof of consent. A contact with no consent record was never asked, which is different from one who said no.

### Step 5: pricing

`PriceResolver.Resolve` is the only way to get a price. It tries each source in order and takes the first that applies:

1. A **contract price** for this customer and offering, valid on the pricing date (the lowest one, if contracts overlap)
2. The customer's own price list, at the best **quantity break** for the quantity asked
3. The default price list, at its best quantity break

Then it takes off the discount and adds VAT. The result is a **price breakdown**: the unit price, gross, discount, VAT, total, the time it was priced, and one `PriceStep` for each decision. This record is what lets an agent explain a price instead of only stating it.

The resolver checks its inputs before it prices. It rejects a customer, offering or price list from another business, a price list that isn't the customer's own, and a contract in another currency. Skipping that contract instead would quietly charge the list price, not the agreed one. Each of these has a test, for example `Rejects_a_list_that_is_not_the_customers_own`.

When there's no price, the result is a failure, never zero (`No_price_is_a_failure_never_zero`). A zero price that reaches a buyer is a free product.

### Step 6: stock and the available date

`StockLevel` holds stock for one offering in one warehouse: `OnHand`, `Allocated` and `OnOrder` (on a purchase order, not yet received), plus `NextReceiptDate`. `Available` is on hand minus allocated.

`AvailableDate` is a simple **ATP**: when can this quantity ship?

```csharp
public DateOnly? AvailableDate(Quantity quantity, DateOnly today)
{
    if (quantity.Value <= Available)
    {
        return today;
    }

    var covered = quantity.Value <= Available + OnOrder;
    return covered && NextReceiptDate is { } receipt && receipt >= today ? receipt : null;
}
```

`null` means "unknown", and the agent must say so rather than guess a date. That's the honesty rule in [PLAN §4](../PLAN.md#4-domain-model): stock and lead-time facts come only from data.

### Step 7: quotes and orders

A **quote** is a numbered offer with priced lines, a validity date (30 days by default) and optional **risk-reducers** such as a return window or phased delivery. `Quote.Create` takes lines that have already been priced and rejects:

- an empty quote, or the same offering on two lines
- a line priced for another customer, or for a different offering
- an offering from another business
- lines in different currencies

Each line keeps a **snapshot** of its price breakdown. If the price list changes tomorrow, the quote doesn't. `QuoteTests.The_snapshot_is_unaffected_by_later_price_and_catalog_changes` proves it. A quote is a commitment, and only a tool-issued document can make one ([ADR-0015](../adr/0015-commitments-only-through-tool-issued-documents.md)).

A **sales order** exists only by converting an accepted quote. `SalesOrder.FromQuote` copies the quoted lines and prices, records the source quote and marks the quote accepted. An expired quote can't be converted. In M1 the order is a skeleton (`Draft → Released → Shipped → Invoiced`). **Holds**, **allocation** and **back-orders** arrive in M4.

## What can go wrong

| Mistake | What happens | The fix here |
|---|---|---|
| `double` for money | 0.1 + 0.2 ≠ 0.3, and invoices are out by a cent | `decimal` inside `Money`, never `double` |
| Rounding only the total | Line totals don't add up to the quote total | Round per line, then sum |
| Treating zero-rated as exempt | Correct total, wrong VAT return | Separate `VatCode`s |
| Returning a price of zero when none is found | A free product | `PricingErrors.NoPrice` failure |
| Skipping a contract in the wrong currency | Silently charging list price | Reject the request |
| Pricing from any price list the caller passes | One customer's prices used for another | Check the list is the customer's own |
| Re-pricing a quote when it's read | The buyer is charged a different price from the one they agreed | Snapshot the breakdown on the line |
| `DateTime.Now` in a rule | Tests pass today and fail at month end | Banned API; time is passed in |
| Throwing for a business rule | The tool can't explain what went wrong | `Result<T>` with a coded `DomainError` |

## Check yourself

1. Why does `Quantity` have both a throwing constructor and a `Create` method that returns a `Result`?
2. A customer has a valid contract price of ZAR 70 and a trade list price of ZAR 85 at their quantity. Which price wins, and which `PriceStep` records it?
3. Why is `AvailableDate` allowed to return `null` instead of an estimate?

**Exercise:** add a rule that a quote can't be issued for a customer who is on hold. Write the failing test in [QuoteTests.cs](../../tests/SalesMcp.Domain.Tests/Quotes/QuoteTests.cs) first, add a `QuoteErrors.CustomerOnHold` error, then make it pass. Should quotes be blocked at all? [erp-order-to-cash.md §4](../research/erp-order-to-cash.md#4-design-decisions-for-the-sales-mcp-domain) says quotes don't count toward credit exposure. Decide, and write down why.

## Code

- [`src/SalesMcp.Domain/`](../../src/SalesMcp.Domain/) and its tests in [`tests/SalesMcp.Domain.Tests/`](../../tests/SalesMcp.Domain.Tests/)
- PRs: building blocks [#51](https://github.com/azharxkhan/sales-mcp/pull/51), VAT [#52](https://github.com/azharxkhan/sales-mcp/pull/52), offerings [#53](https://github.com/azharxkhan/sales-mcp/pull/53), rules [#54](https://github.com/azharxkhan/sales-mcp/pull/54), customers [#56](https://github.com/azharxkhan/sales-mcp/pull/56), pricing [#57](https://github.com/azharxkhan/sales-mcp/pull/57), stock [#58](https://github.com/azharxkhan/sales-mcp/pull/58), quotes and orders [#59](https://github.com/azharxkhan/sales-mcp/pull/59)
