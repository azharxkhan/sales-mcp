using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.AccountNumber).IsRequired();
        builder.Property(c => c.Name).HasMaxLength(CustomerErrors.MaxNameLength).IsRequired();
        builder.Property(c => c.VatNumber);
        builder.Property(c => c.AllowBackOrders);
        builder.Property(c => c.AllowPartialShipment);
        builder.Property(c => c.OnHold);

        builder.ComplexProperty(c => c.CreditLimit, MoneyMapping.Configure);
        builder.ComplexProperty(c => c.Balance, MoneyMapping.Configure);
        builder.ComplexProperty(c => c.PaymentTerms, terms =>
        {
            terms.Property(t => t.Basis).HasColumnName("PaymentTermsBasis");
            terms.Property(t => t.Days).HasColumnName("PaymentTermsDays");
        });

        builder.HasOne<Business>().WithMany().HasForeignKey(c => c.BusinessId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PriceList>().WithMany().HasForeignKey(c => c.PriceListId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => new { c.BusinessId, c.AccountNumber }).IsUnique();
    }
}

internal sealed class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("Contacts");
        builder.HasKey(c => c.Id);
        builder.Ignore(c => c.MayReceiveMarketing);
        builder.Property(c => c.Name).HasMaxLength(CustomerErrors.MaxNameLength).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(CustomerErrors.MaxEmailLength);

        // The latest consent decision lives on the contact's row; all three columns are null when never asked.
        builder.OwnsOne(c => c.MarketingConsent, consent =>
        {
            consent.Property(m => m.OptedIn).HasColumnName("MarketingOptedIn");
            consent.Property(m => m.RecordedAt).HasColumnName("MarketingRecordedAt");
            consent.Property(m => m.Source).HasColumnName("MarketingSource");
        });

        builder.HasOne<Customer>().WithMany().HasForeignKey(c => c.CustomerId).OnDelete(DeleteBehavior.Cascade);
    }
}
