using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Inventory;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Code).HasMaxLength(10).IsRequired();
        builder.Property(w => w.Name).HasMaxLength(InventoryErrors.MaxNameLength).IsRequired();

        builder.HasOne<Business>().WithMany().HasForeignKey(w => w.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(w => new { w.BusinessId, w.Code }).IsUnique();
    }
}

internal sealed class StockLevelConfiguration : IEntityTypeConfiguration<StockLevel>
{
    public void Configure(EntityTypeBuilder<StockLevel> builder)
    {
        builder.ToTable("StockLevels");
        builder.HasKey(s => new { s.OfferingId, s.WarehouseId });
        builder.Ignore(s => s.Available);

        // Stock is counted in the product's unit, which may be metres or kilograms, so 4 decimals.
        builder.Property(s => s.OnHand).HasPrecision(18, 4);
        builder.Property(s => s.Allocated).HasPrecision(18, 4);
        builder.Property(s => s.OnOrder).HasPrecision(18, 4);
        builder.Property(s => s.NextReceiptDate);

        builder.HasOne<Offering>().WithMany().HasForeignKey(s => s.OfferingId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Cascade);
    }
}
