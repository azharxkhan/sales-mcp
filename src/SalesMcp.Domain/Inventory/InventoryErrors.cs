using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Inventory;

/// <summary>Business-rule failures for warehouses and stock. Codes are stable; tools and tests match on them.</summary>
public static class InventoryErrors
{
    /// <summary>Longest allowed warehouse name.</summary>
    public const int MaxNameLength = 200;

    /// <summary>The warehouse code doesn't match the allowed format.</summary>
    public static DomainError WarehouseCodeInvalid { get; } =
        new("WarehouseCodeInvalid", "A warehouse code must be 2 to 10 letters or digits.");

    /// <summary>The name is missing.</summary>
    public static DomainError NameRequired { get; } = new("NameRequired", "A name is required.");

    /// <summary>The name is too long.</summary>
    public static DomainError NameTooLong { get; } =
        new("NameTooLong", $"A name can be at most {MaxNameLength} characters.");

    /// <summary>A stock quantity is negative.</summary>
    public static DomainError QuantityNegative { get; } =
        new("QuantityNegative", "Stock quantities can't be negative.");

    /// <summary>More is allocated than is on hand.</summary>
    public static DomainError OverAllocated { get; } =
        new("OverAllocated", "Allocated stock can't be more than the stock on hand.");

    /// <summary>A receipt date is given with nothing on order.</summary>
    public static DomainError ReceiptWithoutOrder { get; } =
        new("ReceiptWithoutOrder", "A next receipt date needs stock on order.");
}
