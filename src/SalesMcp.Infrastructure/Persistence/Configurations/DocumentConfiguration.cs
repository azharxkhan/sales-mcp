using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Quotes;
using SalesMcp.Infrastructure.Persistence.Conversions;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes");
        builder.HasKey(q => q.Id);
        builder.Property(q => q.Number).IsRequired();
        builder.HasIndex(q => q.Number).IsUnique();
        builder.Property(q => q.AgentId).IsRequired();
        builder.Property(q => q.Currency).HasMaxLength(3).IsRequired();
        builder.Property(q => q.Status);
        builder.Property(q => q.IssuedOn);
        builder.Property(q => q.ValidUntil);

        // What the document states about the seller and buyer, as quoted (ADR-0015).
        builder.Property(q => q.BusinessName).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();
        builder.Property(q => q.CustomerName).HasMaxLength(CustomerErrors.MaxNameLength).IsRequired();
        builder.Property(q => q.AccountNumber).IsRequired();
        builder.ComplexProperty(q => q.PaymentTerms, terms =>
        {
            terms.Property(t => t.Basis).HasColumnName("PaymentTermsBasis");
            terms.Property(t => t.Days).HasColumnName("PaymentTermsDays");
        });
        builder.Property(q => q.RiskReducers)
            .HasConversion(new RiskReducersConverter(), RiskReducersConverter.Comparer)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasOne<Customer>().WithMany().HasForeignKey(q => q.CustomerId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(q => q.Lines).WithOne().HasForeignKey("QuoteId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(q => q.Lines).HasField("_lines").AutoInclude();
    }
}

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable("SalesOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Number).IsRequired();
        builder.HasIndex(o => o.Number).IsUnique();
        builder.Property(o => o.AgentId).IsRequired();
        builder.Property(o => o.Currency).HasMaxLength(3).IsRequired();
        builder.Property(o => o.Status);
        builder.Property(o => o.OrderedOn);

        builder.HasOne<Customer>().WithMany().HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.Restrict);

        // One order per quote: the database backs up Quote.MarkAccepted.
        builder.HasOne<Quote>().WithMany().HasForeignKey(o => o.SourceQuoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => o.SourceQuoteId).IsUnique();

        builder.HasMany(o => o.Lines).WithOne().HasForeignKey("OrderId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(o => o.Lines).HasField("_lines").AutoInclude();
    }
}

internal sealed class QuoteLineConfiguration : IEntityTypeConfiguration<QuoteLine>
{
    public void Configure(EntityTypeBuilder<QuoteLine> builder)
    {
        builder.ToTable("QuoteLines");
        builder.Property<QuoteId>("QuoteId");
        builder.HasKey("QuoteId", nameof(QuoteLine.LineNumber));
        builder.Property(l => l.LineNumber).ValueGeneratedNever();
        builder.Property(l => l.Sku).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();
        builder.Property(l => l.Unit);

        // The price as quoted, kept whole (ADR-0015). Never recalculated.
        builder.Property(l => l.Price).HasConversion<PriceBreakdownConverter>().HasColumnName("PriceSnapshot").IsRequired();

        builder.Ignore(l => l.Quantity);
        builder.Ignore(l => l.Net);
        builder.Ignore(l => l.Vat);
        builder.Ignore(l => l.Total);
        builder.HasOne<Offering>().WithMany().HasForeignKey(l => l.OfferingId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> builder)
    {
        builder.ToTable("OrderLines");
        builder.Property<OrderId>("OrderId");
        builder.HasKey("OrderId", nameof(OrderLine.LineNumber));
        builder.Property(l => l.LineNumber).ValueGeneratedNever();
        builder.Property(l => l.Sku).IsRequired();
        builder.Property(l => l.Name).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();
        builder.ComplexProperty(l => l.UnitPrice, MoneyMapping.Configure);
        builder.Property(l => l.Vat).HasConversion<VatLineConverter>().HasColumnName("VatSnapshot").IsRequired();
        builder.Ignore(l => l.Net);
        builder.Ignore(l => l.Total);
        builder.HasOne<Offering>().WithMany().HasForeignKey(l => l.OfferingId).OnDelete(DeleteBehavior.Restrict);
    }
}
