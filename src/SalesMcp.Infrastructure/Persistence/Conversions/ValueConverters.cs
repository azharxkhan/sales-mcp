using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Quotes;

namespace SalesMcp.Infrastructure.Persistence.Conversions;

// Typed ids are stored as their Guid.

internal sealed class BusinessIdConverter() : ValueConverter<BusinessId, Guid>(v => v.Value, v => new BusinessId(v));

internal sealed class OfferingIdConverter() : ValueConverter<OfferingId, Guid>(v => v.Value, v => new OfferingId(v));

internal sealed class CustomerIdConverter() : ValueConverter<CustomerId, Guid>(v => v.Value, v => new CustomerId(v));

internal sealed class ContactIdConverter() : ValueConverter<ContactId, Guid>(v => v.Value, v => new ContactId(v));

internal sealed class PriceListIdConverter() : ValueConverter<PriceListId, Guid>(v => v.Value, v => new PriceListId(v));

internal sealed class WarehouseIdConverter() : ValueConverter<WarehouseId, Guid>(v => v.Value, v => new WarehouseId(v));

internal sealed class QuoteIdConverter() : ValueConverter<QuoteId, Guid>(v => v.Value, v => new QuoteId(v));

internal sealed class OrderIdConverter() : ValueConverter<OrderId, Guid>(v => v.Value, v => new OrderId(v));

// Validated value types are stored as text and re-validated on the way back in. Bad data in the
// database fails loudly rather than producing an invalid domain object.

internal sealed class SkuConverter() : ValueConverter<Sku, string>(v => v.Value, v => Sku.Create(v).Value);

internal sealed class AccountNumberConverter()
    : ValueConverter<AccountNumber, string>(v => v.Value, v => AccountNumber.Create(v).Value);

internal sealed class VatNumberConverter() : ValueConverter<VatNumber, string>(v => v.Value, v => VatNumber.Create(v).Value);

internal sealed class AgentIdConverter() : ValueConverter<AgentId, string>(v => v.Value, v => AgentId.Create(v).Value);

internal sealed class OrderNumberConverter()
    : ValueConverter<OrderNumber, string>(v => v.Value, v => OrderNumber.Parse(v).Value);

internal sealed class QuoteNumberConverter()
    : ValueConverter<QuoteNumber, string>(v => v.Value, v => QuoteNumber.Parse(v).Value);

// Numeric value types are stored as decimals.

internal sealed class QuantityConverter() : ValueConverter<Quantity, decimal>(v => v.Value, v => new Quantity(v));

internal sealed class PercentageConverter() : ValueConverter<Percentage, decimal>(v => v.Value, v => new Percentage(v));

/// <summary>Stores risk-reducers as a comma-separated list of names, for example <c>Trial,ReturnWindow</c>.</summary>
internal sealed class RiskReducersConverter() : ValueConverter<IReadOnlyList<RiskReducer>, string>(
    v => string.Join(',', v),
    v => Parse(v))
{
    public static ValueComparer<IReadOnlyList<RiskReducer>> Comparer { get; } = new(
        (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
        v => v.Aggregate(0, (hash, r) => HashCode.Combine(hash, r)),
        v => v.ToList());

    private static List<RiskReducer> Parse(string value) =>
        value.Length == 0
            ? []
            : [.. value.Split(',').Select(name => Enum.Parse<RiskReducer>(name))];
}
