using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SalesMcp.Domain.Common;

namespace SalesMcp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Money is a complex property: two columns, <c>{Name}_Amount</c> and <c>{Name}_Currency</c>.
/// The currency is required, because <c>default(Money)</c> has none.
/// </summary>
internal static class MoneyMapping
{
    public static void Configure(ComplexPropertyBuilder<Money> money)
    {
        money.Property(m => m.Amount).HasPrecision(SalesDbContext.MoneyPrecision, SalesDbContext.MoneyScale);
        money.Property(m => m.Currency).HasMaxLength(3).IsRequired();
        money.Ignore(m => m.IsZero);
        money.Ignore(m => m.IsNegative);
    }
}
