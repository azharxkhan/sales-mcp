using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Catalog;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.ToTable("Businesses");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();
        builder.Property(b => b.Currency).HasMaxLength(3).IsRequired();
        builder.Property(b => b.Locale).HasMaxLength(5).IsRequired();
    }
}

/// <summary>All three kinds in one table (table-per-hierarchy), so the catalog is searched in one place.</summary>
internal sealed class OfferingConfiguration : IEntityTypeConfiguration<Offering>
{
    public void Configure(EntityTypeBuilder<Offering> builder)
    {
        builder.ToTable("Offerings");
        builder.HasKey(o => o.Id);
        builder.Ignore(o => o.Kind);

        builder.HasDiscriminator<string>("Discriminator")
            .HasValue<Product>(nameof(OfferingKind.Product))
            .HasValue<Service>(nameof(OfferingKind.Service))
            .HasValue<Plan>(nameof(OfferingKind.Plan));
        builder.Property("Discriminator").HasColumnName("Kind").HasMaxLength(16);

        builder.Property(o => o.Sku).IsRequired();
        builder.Property(o => o.Name).HasMaxLength(CatalogErrors.MaxNameLength).IsRequired();
        builder.Property(o => o.Description).HasMaxLength(CatalogErrors.MaxDescriptionLength).IsRequired();
        builder.Property(o => o.VatCode);
        builder.Property(o => o.IsActive);

        builder.HasOne<Business>().WithMany().HasForeignKey(o => o.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(o => new { o.BusinessId, o.Sku }).IsUnique();
    }
}

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Ignore(p => p.HasReturnWindow);
        builder.Property(p => p.Unit);
        builder.Property(p => p.LeadTimeDays);
        builder.Property(p => p.ReturnWindowDays);
    }
}

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder) => builder.Property(s => s.PricingModel);
}

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder) => builder.Property(p => p.BillingPeriod);
}

internal sealed class OfferingRuleConfiguration : IEntityTypeConfiguration<OfferingRule>
{
    public void Configure(EntityTypeBuilder<OfferingRule> builder)
    {
        builder.ToTable("OfferingRules");

        // At most one rule per ordered pair (OfferingRuleSet enforces the rest when rules are added).
        builder.HasKey(r => new { r.OfferingId, r.TargetOfferingId });
        builder.Property(r => r.Kind);
        builder.Property(r => r.Reason).HasMaxLength(CatalogErrors.MaxReasonLength).IsRequired();

        builder.HasOne<Offering>().WithMany().HasForeignKey(r => r.OfferingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Offering>().WithMany().HasForeignKey(r => r.TargetOfferingId).OnDelete(DeleteBehavior.Restrict);
    }
}
