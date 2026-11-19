using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Catalog;

/// <summary>The unit a product is sold in. Quantities on quotes and orders are in this unit.</summary>
public enum UnitOfMeasure
{
    /// <summary>A single item.</summary>
    Each = 0,

    /// <summary>A pair, for example gloves or boots.</summary>
    Pair = 1,

    /// <summary>A box of a fixed count.</summary>
    Box = 2,

    /// <summary>A pack of a fixed count.</summary>
    Pack = 3,

    /// <summary>A roll, for example tape or cable.</summary>
    Roll = 4,

    /// <summary>Metres, for cut-to-length goods.</summary>
    Metre = 5,

    /// <summary>Litres, for liquids such as lubricants.</summary>
    Litre = 6,

    /// <summary>Kilograms, for goods sold by weight.</summary>
    Kilogram = 7,
}

/// <summary>Physical goods, sold from stock (stock levels arrive with the inventory model).</summary>
public sealed class Product : Offering
{
#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private Product()
    {
    }
#pragma warning restore CS8618

    private Product(
        OfferingId id,
        BusinessId businessId,
        CommonFields common,
        VatCode vatCode,
        UnitOfMeasure unit,
        int leadTimeDays,
        int returnWindowDays)
        : base(id, businessId, common.Sku, common.Name, common.Description, vatCode)
    {
        Unit = unit;
        LeadTimeDays = leadTimeDays;
        ReturnWindowDays = returnWindowDays;
    }

    /// <inheritdoc />
    public override OfferingKind Kind => OfferingKind.Product;

    /// <summary>The unit it is sold in.</summary>
    public UnitOfMeasure Unit { get; }

    /// <summary>Days to replenish when out of stock. Used for honest availability dates, never for pressure.</summary>
    public int LeadTimeDays { get; }

    /// <summary>Days within which it can be returned. 0 means no returns.</summary>
    public int ReturnWindowDays { get; }

    /// <summary>
    /// True when a return window exists. A "return window" risk-reducer may only be offered
    /// for products where this is true (guardrail B3).
    /// </summary>
    public bool HasReturnWindow => ReturnWindowDays > 0;

    /// <summary>Validates input and creates an active product.</summary>
    public static Result<Product> Create(
        OfferingId id,
        BusinessId businessId,
        string? sku,
        string? name,
        string? description,
        VatCode vatCode,
        UnitOfMeasure unit,
        int leadTimeDays,
        int returnWindowDays)
    {
        if (!Enum.IsDefined(unit))
        {
            return Result.Failure<Product>(CatalogErrors.OptionUnknown);
        }

        if (leadTimeDays < 0 || returnWindowDays < 0)
        {
            return Result.Failure<Product>(CatalogErrors.DaysNegative);
        }

        return ValidateCommon(sku, name, description, vatCode)
            .Map(common => new Product(id, businessId, common, vatCode, unit, leadTimeDays, returnWindowDays));
    }
}
