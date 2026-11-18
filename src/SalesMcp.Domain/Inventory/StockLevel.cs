using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Inventory;

/// <summary>
/// How much of one product a warehouse holds, has promised and expects. Read-only in M1; allocation
/// at order release arrives in M4.
/// </summary>
/// <remarks>
/// Stock facts reach the buyer only from here, with an "as of" time (guardrail B1). The agent can't
/// claim scarcity or a delivery date these figures don't support.
/// </remarks>
public sealed class StockLevel
{
    private StockLevel(
        OfferingId offeringId,
        WarehouseId warehouseId,
        decimal onHand,
        decimal allocated,
        decimal onOrder,
        DateOnly? nextReceiptDate)
    {
        OfferingId = offeringId;
        WarehouseId = warehouseId;
        OnHand = onHand;
        Allocated = allocated;
        OnOrder = onOrder;
        NextReceiptDate = nextReceiptDate;
    }

    /// <summary>The product.</summary>
    public OfferingId OfferingId { get; }

    /// <summary>The warehouse.</summary>
    public WarehouseId WarehouseId { get; }

    /// <summary>Physically in the warehouse.</summary>
    public decimal OnHand { get; }

    /// <summary>Promised to released orders but not yet shipped.</summary>
    public decimal Allocated { get; }

    /// <summary>Expected from open purchase orders.</summary>
    public decimal OnOrder { get; }

    /// <summary>When the next purchase order is due in, or null if unknown.</summary>
    public DateOnly? NextReceiptDate { get; }

    /// <summary>What can still be promised now: on hand less allocated.</summary>
    public decimal Available => OnHand - Allocated;

    /// <summary>Validates and creates a stock level.</summary>
    public static Result<StockLevel> Create(
        OfferingId offeringId,
        WarehouseId warehouseId,
        decimal onHand,
        decimal allocated,
        decimal onOrder,
        DateOnly? nextReceiptDate)
    {
        if (onHand < 0m || allocated < 0m || onOrder < 0m)
        {
            return Result.Failure<StockLevel>(InventoryErrors.QuantityNegative);
        }

        if (allocated > onHand)
        {
            return Result.Failure<StockLevel>(InventoryErrors.OverAllocated);
        }

        return nextReceiptDate is not null && onOrder == 0m
            ? Result.Failure<StockLevel>(InventoryErrors.ReceiptWithoutOrder)
            : Result.Success(new StockLevel(offeringId, warehouseId, onHand, allocated, onOrder, nextReceiptDate));
    }

    /// <summary>
    /// The earliest date <paramref name="quantity"/> can be promised (a simple available-to-promise):
    /// <paramref name="today"/> if it is available now; the next receipt date if what's available plus
    /// what's on order covers it; otherwise null, meaning unknown.
    /// </summary>
    /// <remarks>
    /// A receipt date already in the past means the purchase order is late, so the answer is unknown
    /// rather than a date nobody can stand behind. Unknown must be reported as unknown, never guessed.
    /// </remarks>
    /// <param name="quantity">How many the buyer wants.</param>
    /// <param name="today">Today's date in the business's time zone, from the caller's clock.</param>
    public DateOnly? AvailableDate(Quantity quantity, DateOnly today)
    {
        if (quantity.Value <= Available)
        {
            return today;
        }

        var covered = quantity.Value <= Available + OnOrder;
        return covered && NextReceiptDate is { } receipt && receipt >= today ? receipt : null;
    }
}
