using Microsoft.EntityFrameworkCore;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Inventory;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Requests;
using SalesMcp.Infrastructure.Persistence.Conversions;

namespace SalesMcp.Infrastructure.Persistence;

/// <summary>
/// The EF Core context for the sales model (ADR-0005). One configuration class per aggregate lives in
/// <c>Persistence/Configurations</c>; conventions here cover typed ids, value types and decimals.
/// </summary>
public sealed class SalesDbContext(DbContextOptions<SalesDbContext> options) : DbContext(options)
{
    /// <summary>Precision for money amounts: 19 digits, 4 after the point, so unit prices like 33.335 survive.</summary>
    public const int MoneyPrecision = 19;

    /// <summary>Scale for money amounts.</summary>
    public const int MoneyScale = 4;

    /// <summary>Businesses.</summary>
    public DbSet<Business> Businesses => Set<Business>();

    /// <summary>Offerings of every kind (one table, discriminated by kind).</summary>
    public DbSet<Offering> Offerings => Set<Offering>();

    /// <summary>Configuration rules between offerings.</summary>
    public DbSet<OfferingRule> OfferingRules => Set<OfferingRule>();

    /// <summary>Customers.</summary>
    public DbSet<Customer> Customers => Set<Customer>();

    /// <summary>Contacts at customers.</summary>
    public DbSet<Contact> Contacts => Set<Contact>();

    /// <summary>Price lists, with their items.</summary>
    public DbSet<PriceList> PriceLists => Set<PriceList>();

    /// <summary>Contract prices.</summary>
    public DbSet<ContractPrice> ContractPrices => Set<ContractPrice>();

    /// <summary>Warehouses.</summary>
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

    /// <summary>Stock levels per offering and warehouse.</summary>
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();

    /// <summary>Quotes, with their lines.</summary>
    public DbSet<Quote> Quotes => Set<Quote>();

    /// <summary>Sales orders, with their lines.</summary>
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();

    /// <summary>First results of mutating tool calls, by request id (guardrail A8).</summary>
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);

        configurationBuilder.Properties<BusinessId>().HaveConversion<BusinessIdConverter>();
        configurationBuilder.Properties<OfferingId>().HaveConversion<OfferingIdConverter>();
        configurationBuilder.Properties<CustomerId>().HaveConversion<CustomerIdConverter>();
        configurationBuilder.Properties<ContactId>().HaveConversion<ContactIdConverter>();
        configurationBuilder.Properties<PriceListId>().HaveConversion<PriceListIdConverter>();
        configurationBuilder.Properties<WarehouseId>().HaveConversion<WarehouseIdConverter>();
        configurationBuilder.Properties<QuoteId>().HaveConversion<QuoteIdConverter>();
        configurationBuilder.Properties<OrderId>().HaveConversion<OrderIdConverter>();

        configurationBuilder.Properties<Sku>().HaveConversion<SkuConverter>().HaveMaxLength(32);
        configurationBuilder.Properties<AccountNumber>().HaveConversion<AccountNumberConverter>().HaveMaxLength(20);
        configurationBuilder.Properties<VatNumber>().HaveConversion<VatNumberConverter>().HaveMaxLength(10);
        configurationBuilder.Properties<AgentId>().HaveConversion<AgentIdConverter>().HaveMaxLength(64);
        configurationBuilder.Properties<QuoteNumber>().HaveConversion<QuoteNumberConverter>().HaveMaxLength(13);

        // Quantities allow 4 decimals (metres, litres, kilograms); percentages allow 2.
        if (Database.IsSqlite())
        {
            // SQLite has no decimal type: store scaled integers (see ScaledInteger for why).
            configurationBuilder.Properties<decimal>().HaveConversion<ScaledDecimalConverter>();
            configurationBuilder.Properties<Quantity>().HaveConversion<ScaledQuantityConverter>();
            configurationBuilder.Properties<Percentage>().HaveConversion<ScaledPercentageConverter>();
        }
        else
        {
            configurationBuilder.Properties<Quantity>().HaveConversion<QuantityConverter>().HavePrecision(18, 4);
            configurationBuilder.Properties<Percentage>().HaveConversion<PercentageConverter>().HavePrecision(5, 2);
            configurationBuilder.Properties<decimal>().HavePrecision(MoneyPrecision, MoneyScale);
        }

        // Enums are stored by name so the database stays readable and reordering an enum can't corrupt data.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SalesDbContext).Assembly);
    }
}
