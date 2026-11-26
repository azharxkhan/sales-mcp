using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Inventory;

/// <summary>Available-to-promise across every warehouse that holds a product.</summary>
public static class StockPosition
{
    /// <summary>
    /// The earliest date <paramref name="quantity"/> can be promised from all warehouses together:
    /// <paramref name="today"/> if the available stock covers it; otherwise the first receipt date by
    /// which available stock plus stock due in covers it; otherwise null, meaning unknown.
    /// </summary>
    /// <remarks>
    /// Receipt dates in the past are late purchase orders, so their stock is not counted, as in
    /// <see cref="StockLevel.AvailableDate"/>. Unknown must be reported as unknown, never guessed.
    /// </remarks>
    /// <param name="levels">The product's stock levels, one per warehouse.</param>
    /// <param name="quantity">How many the buyer wants.</param>
    /// <param name="today">Today's date in the business's time zone, from the caller's clock.</param>
    public static DateOnly? AvailableDate(IEnumerable<StockLevel> levels, Quantity quantity, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(levels);

        var list = levels.ToList();
        var covered = list.Sum(l => l.Available);
        if (quantity.Value <= covered)
        {
            return today;
        }

        var receipts = list
            .Where(l => l.NextReceiptDate is { } date && date >= today)
            .OrderBy(l => l.NextReceiptDate);

        foreach (var level in receipts)
        {
            covered += level.OnOrder;
            if (quantity.Value <= covered)
            {
                return level.NextReceiptDate;
            }
        }

        return null;
    }
}
