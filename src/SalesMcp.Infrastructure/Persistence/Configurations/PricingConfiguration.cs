using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
    public void Configure(EntityTypeBuilder<PriceList> builder)
    {
        builder.ToTable("PriceLists");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(PricingErrors.MaxNameLength).IsRequired();
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.IsDefault);

        builder.HasOne<Business>().WithMany().HasForeignKey(p => p.BusinessId).OnDelete(DeleteBehavior.Restrict);

        // One default list per business.
        builder.HasIndex(p => p.BusinessId).IsUnique().HasFilter("\"IsDefault\" = 1").HasDatabaseName("IX_PriceLists_DefaultPerBusiness");

        builder.HasMany(p => p.Items).WithOne().HasForeignKey("PriceListId").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Items).HasField("_items").AutoInclude();
    }
}

internal sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
    public void Configure(EntityTypeBuilder<PriceListItem> builder)
    {
        builder.ToTable("PriceListItems");
        builder.Property<PriceListId>("PriceListId");
        builder.HasKey("PriceListId", nameof(PriceListItem.OfferingId), nameof(PriceListItem.MinQuantity));
        builder.ComplexProperty(i => i.UnitPrice, MoneyMapping.Configure);
        builder.HasOne<Offering>().WithMany().HasForeignKey(i => i.OfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ContractPriceConfiguration : IEntityTypeConfiguration<ContractPrice>
{
    public void Configure(EntityTypeBuilder<ContractPrice> builder)
    {
        builder.ToTable("ContractPrices");
        builder.HasKey(c => new { c.CustomerId, c.OfferingId, c.ValidFrom });
        builder.ComplexProperty(c => c.UnitPrice, MoneyMapping.Configure);
        builder.Property(c => c.ValidTo);

        builder.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Offering>().WithMany().HasForeignKey(c => c.OfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}
