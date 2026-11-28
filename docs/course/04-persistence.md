# Chapter 4: Persistence

> **Milestone:** M1 · **Code:** PRs [#60](https://github.com/azharxkhan/sales-mcp/pull/60), [#61](https://github.com/azharxkhan/sales-mcp/pull/61) and [#62](https://github.com/azharxkhan/sales-mcp/pull/62) · **Previous:** [Chapter 3: Modelling a sales domain](03-modelling-a-sales-domain.md)

## What you'll build

The chapter 3 domain stored in SQLite with EF Core 10: mappings, a first migration, and a realistic, repeatable demo business called **Acme Distributors**. At the end, an integration test migrates a fresh database, seeds 120 products, 40 customers and a year of quotes and orders, and reads them back with every cent intact.

```text
SeededAcmeDatabase: migrate → seed → read back
  14 tables, row counts match the in-memory dataset
  Money and stock sums in SQLite = sums in memory, exactly
  Seed again: nothing changes
```

## Why it matters

Chapter 3 kept the business rules free of any database. That only pays off if persistence can be added from the outside without bending the rules to fit it. This chapter shows how, and where a small compromise was needed.

It's also where the project's first real bug turned up. It passed on Linux CI and failed on a South African laptop. The fix, and the test that guards it, are the main lesson here.

Demo data matters as much as the schema. An agent tested against five tidy rows behaves well in a demo and badly with real customers. The seed is shaped by published B2B benchmarks ([sales-data-benchmarks.md](../research/sales-data-benchmarks.md)), so tools and evaluations in later chapters meet realistic data.

## Concepts

**Mapping lives in Infrastructure.** Domain classes carry no EF attributes. Each aggregate has an `IEntityTypeConfiguration<T>` in [`Persistence/Configurations/`](../../src/SalesMcp.Infrastructure/Persistence/Configurations/). The one concession to EF is a private parameterless constructor on entities, marked *for EF Core only*. Domain still references no EF assembly, and `LayeringTests` fail if it ever does.

**Application owns the interfaces.** Use cases see the store through narrow interfaces in [`Application/Persistence/`](../../src/SalesMcp.Application/Persistence/): `ICatalogReader`, `ICustomerReader`, `IPricingReader`, `IStockReader`, `IQuoteRepository`, `IOrderRepository` and `IUnitOfWork`. Each method exists because a use case needs it. There's no generic repository. Readers return untracked entities, because a read-only tool shouldn't be able to save by accident.

**SQLite now, PostgreSQL later.** SQLite needs no server, so a learner can clone and run the project at once. PostgreSQL arrives with Docker in M5 ([ADR-0005](../adr/0005-ef-core-sqlite-postgres.md)). Anything provider-specific is behind `Database.IsSqlite()`.

**A migration is code you review.** `dotnet ef migrations add` generates it from the model. It's committed, reviewed like any other change, and a test fails if the model and the migrations drift apart.

## Step by step

### Step 1: the context and conventions

[`SalesDbContext`](../../src/SalesMcp.Infrastructure/Persistence/SalesDbContext.cs) declares one `DbSet` per aggregate root and applies every configuration in the assembly. Conventions handle the value objects once for the whole model:

| Domain type | Stored as |
|---|---|
| Typed ids (`CustomerId`, …) | The `Guid` |
| `Sku`, `AccountNumber`, `VatNumber`, `AgentId`, `QuoteNumber` | Text, validated again when read, so bad data fails loudly |
| Enums | Their name, so the database is readable and reordering an enum can't corrupt data |
| `Money` | A complex property: `{Name}_Amount` and `{Name}_Currency` columns |
| `decimal`, `Quantity`, `Percentage` | Scaled integers on SQLite (step 2), native `numeric` elsewhere |

Some mappings worth reading:

- **Offerings** use one table for all three kinds (table per hierarchy), with a `Kind` column saying which.
- **Quote lines** store their price breakdown as JSON in a `PriceSnapshot` column, exactly as quoted ([ADR-0015](../adr/0015-commitments-only-through-tool-issued-documents.md)). JSON DTOs live in [`SnapshotJson.cs`](../../src/SalesMcp.Infrastructure/Persistence/Conversions/SnapshotJson.cs), so the domain needs no serializer attributes. The other values the document states (names, account number, payment terms, units) are ordinary columns on `Quotes` and `QuoteLines`, copied when the quote is issued.
- **The database backs up the rules.** Unique indexes allow one order per quote and one default price list per business. If a bug in the code ever lets a second one through, the save fails.

### Step 2: SQLite and decimals

SQLite has no decimal type. By default EF stores decimals as text and compares them with a collation that parses using the **current culture**. In `en-ZA` the decimal separator is a comma, so `ORDER BY` on a money column threw `FormatException: '2.0'`. CI on Linux runs in the invariant culture and passed. Every South African user would have hit it.

The fix stores decimals as **scaled integers**: money, stock and quantities × 10 000, percentages × 100. SQLite sorts, compares and sums integers natively and exactly, with no culture involved:

```csharp
public static long ToStored(decimal value, int scale)
{
    var factor = Factor(scale);
    var scaled = value * factor;
    if (scaled != decimal.Truncate(scaled))
    {
        throw new InvalidOperationException(
            $"{value} has more than {scale} decimal places and can't be stored without rounding.");
    }

    return decimal.ToInt64(scaled);
}
```

A value with more than 4 decimals is **rejected, not rounded**. Silent rounding in storage is how a quote ends up a cent different from the invoice. Two tests guard this: `Sorts_and_sums_money_numerically_on_sqlite`, and `Money_and_stock_sums_on_sqlite_equal_the_in_memory_sums` over the whole seed. Both switch the culture to `en-ZA` themselves, so they fail on any machine if the bug comes back.

### Step 3: the first migration

The `dotnet ef` tool needs a context at design time. [`DesignTimeSalesDbContextFactory`](../../src/SalesMcp.Infrastructure/Persistence/DesignTimeSalesDbContextFactory.cs) gives it one on an in-memory SQLite database, so no connection string is needed to generate migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/SalesMcp.Infrastructure --output-dir Migrations/Sqlite
```

The result is in [`Migrations/Sqlite/`](../../src/SalesMcp.Infrastructure/Migrations/Sqlite/). `PersistenceTests.The_migration_matches_the_model` fails if you change the model and forget to add a migration.

### Step 4: wiring

`AddSalesInfrastructure(connectionString)` in [`DependencyInjection.cs`](../../src/SalesMcp.Infrastructure/DependencyInjection.cs) registers the context and every interface as **scoped**. One scope is one unit of work. In M2, one tool call will be one scope.

### Step 5: Acme Distributors

[`AcmeSeed.Build(today)`](../../src/SalesMcp.Infrastructure/Seeding/AcmeSeed.cs) builds an invented South African industrial-supplies distributor in memory:

- 120 products in 8 categories, covering all three VAT codes, and 15 offering rules
- 2 warehouses (Johannesburg and Durban), filled to about 97%, with some stock on order
- 40 customers whose revenue follows a Pareto shape: a few large accounts and many small ones
- a default price list with quantity breaks, mining and trade lists, and contracts for the top 5 customers
- 12 months of quotes and orders, winning about 20% of quotes from new customers and 45% from existing ones

Four choices make it safe to build on:

1. **Repeatable.** Randomness comes from `new Random(AcmeSeed.RandomSeed)`. The same `today` always gives the same rows (`Is_the_same_every_time_for_the_same_day`).
2. **Never stale.** Dates are relative to `today`, so open quotes stay open and receipt dates stay in the future, whenever you run it.
3. **Built through the domain.** Every row is created with the same factories the tools will use, so seed data can't break a rule that live data must follow.
4. **Invented.** No real companies or people. Contacts use reserved email domains (guardrail E2 in [guardrails.md](../research/guardrails.md#4-guardrail-catalogue)).

Tests need stable rows to point at: the top customer, a customer near their credit limit, a product that's out of stock but has a receipt date. [`SeedManifest`](../../src/SalesMcp.Infrastructure/Seeding/SeedManifest.cs) names them. Their ids come from a hash of a fixed key, such as `acme/customer/…`, so they're the same on every run and every machine.

[`AcmeSeeder.SeedAsync`](../../src/SalesMcp.Infrastructure/Seeding/AcmeSeeder.cs) writes the dataset in one save, and does nothing if Acme already exists. That makes it safe to call at every start-up.

### Step 6: integration tests

[`SqliteDatabase`](../../tests/SalesMcp.IntegrationTests/Persistence/SqliteDatabase.cs) creates a private in-memory database (a uniquely named, shared-cache SQLite database), kept alive by one open connection, and migrates it with the real migrations. `InScopeAsync` runs each piece of work in a fresh scope, as one tool call would, so a test can't pass by reading back what EF still holds in memory.

[`SeededAcmeDatabase`](../../tests/SalesMcp.IntegrationTests/Seeding/SeededAcmeDatabase.cs) is an xUnit class fixture. It migrates and seeds once per test class, and every test in [`AcmeSeederTests`](../../tests/SalesMcp.IntegrationTests/Seeding/AcmeSeederTests.cs) only reads. Those tests check that:

- every table holds exactly as many rows as the in-memory dataset
- money and stock sums in SQLite equal the in-memory sums
- every quote and order reads back with the same status, lines and VAT totals
- seeding again, on the same day or the next, writes nothing
- the named fixtures read back through the Application interfaces

## What can go wrong

| Mistake | What happens | The fix here |
|---|---|---|
| EF's default decimal mapping on SQLite | Sorting money throws on `en-ZA`, but passes on CI | Scaled integers; the test runs under `en-ZA` |
| Rounding when storing | A cent lost between quote and invoice | Reject more than 4 decimals |
| EF attributes on domain classes | Domain depends on EF; layering breaks | Fluent configurations in Infrastructure; architecture test |
| Re-pricing quote lines when they're loaded | A different price from the one the buyer agreed | Store the breakdown as a JSON snapshot |
| Enums stored as numbers | Reordering an enum changes old data | Store the name |
| Forgetting a migration | Works in tests that use `EnsureCreated`, fails in production | Tests use real migrations and check for drift |
| Random seed data | Flaky tests, different demos every run | Fixed random seed, ids from hashed keys |
| Seed data built with `new` and setters | Seed rows break rules real data can't | Build through domain factories |
| Seeding on every start-up without a check | Duplicate customers | Skip if the business exists; tested |
| Asserting on the same context that saved | The test reads EF's memory, not the database | Fresh scope per step |

## Check yourself

1. Why would the decimal bug pass on CI but fail on a laptop in Durban?
2. Why does the seeder check for the business, not count the customers?
3. A quote line stores its price as JSON instead of in columns. What do you lose, and why is it worth it here?

**Exercise:** add a `Notes` field (up to 500 characters) to `Customer`. Add it to the domain factory and to `CustomerConfiguration`. Run the tests and watch `The_migration_matches_the_model` fail. Then add the migration with `dotnet ef migrations add AddCustomerNotes` and make it pass. Read the generated migration before you commit it.

## Code

- [`src/SalesMcp.Infrastructure/`](../../src/SalesMcp.Infrastructure/) and [`src/SalesMcp.Application/Persistence/`](../../src/SalesMcp.Application/Persistence/)
- Tests: [`tests/SalesMcp.IntegrationTests/`](../../tests/SalesMcp.IntegrationTests/)
- PRs: EF Core context, mappings and migration [#60](https://github.com/azharxkhan/sales-mcp/pull/60), Acme seed data [#61](https://github.com/azharxkhan/sales-mcp/pull/61), SQLite integration tests [#62](https://github.com/azharxkhan/sales-mcp/pull/62)
