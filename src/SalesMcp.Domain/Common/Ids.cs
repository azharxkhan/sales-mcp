namespace SalesMcp.Domain.Common;

// Strongly typed ids. Each wraps a Guid so a CustomerId can never be passed where an OfferingId
// is expected. New ids are version 7 Guids (time-ordered), which index well in databases.
// Human-facing numbers (quote numbers, SKUs, account numbers) are separate value types.

/// <summary>Identifies a business (a seller with its own catalog).</summary>
public readonly record struct BusinessId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static BusinessId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies an offering (product, service or plan).</summary>
public readonly record struct OfferingId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static OfferingId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a customer account.</summary>
public readonly record struct CustomerId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static CustomerId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a contact person at a customer.</summary>
public readonly record struct ContactId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static ContactId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a price list.</summary>
public readonly record struct PriceListId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static PriceListId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a warehouse.</summary>
public readonly record struct WarehouseId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static WarehouseId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a quote.</summary>
public readonly record struct QuoteId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static QuoteId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}

/// <summary>Identifies a sales order.</summary>
public readonly record struct OrderId(Guid Value)
{
    /// <summary>A new, time-ordered id.</summary>
    public static OrderId New() => new(Guid.CreateVersion7());

    /// <inheritdoc />
    public override string ToString() => Value.ToString("D");
}
