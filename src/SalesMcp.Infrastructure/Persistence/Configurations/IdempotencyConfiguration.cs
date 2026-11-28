using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Requests;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        // One record per key, tool and customer (guardrail A8): the key alone is the client's, so
        // scoping it by customer keeps one customer's key from ever reaching another's result.
        builder.HasKey(r => new { r.Key, r.Tool, r.CustomerId });
        builder.Property(r => r.Key).HasMaxLength(IdempotencyRecord.MaxKeyLength);
        builder.Property(r => r.Tool).HasMaxLength(IdempotencyRecord.MaxToolLength);
        builder.Property(r => r.RequestHash).HasMaxLength(IdempotencyRecord.HashLength).IsRequired();
        builder.Property(r => r.ResponseJson).IsRequired();
        builder.Property(r => r.CreatedAt);

        builder.HasOne<Customer>().WithMany().HasForeignKey(r => r.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
