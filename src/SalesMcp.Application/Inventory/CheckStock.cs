using SalesMcp.Application.Catalog;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Inventory;

namespace SalesMcp.Application.Inventory;

/// <summary>When the requested quantity can be promised.</summary>
public enum StockOutlook
{
    /// <summary>Available now.</summary>
    Now = 0,

    /// <summary>Not available now, but stock due in covers it by a known date.</summary>
    Later = 1,

    /// <summary>Neither stock nor known receipts cover it, so nobody can say when.</summary>
    Unknown = 2,
}

/// <summary>One warehouse's figures for the product.</summary>
/// <param name="Code">The warehouse code, for example <c>JHB</c>.</param>
/// <param name="Name">The warehouse name, cleaned as untrusted text.</param>
/// <param name="Available">On hand less allocated.</param>
/// <param name="OnOrder">Due in on open purchase orders.</param>
/// <param name="NextReceiptDate">When the next purchase order is due, if known.</param>
/// <param name="AvailableDate">When this warehouse alone can supply the quantity, or null if unknown.</param>
public sealed record WarehouseStock(
    string Code,
    string Name,
    decimal Available,
    decimal OnOrder,
    DateOnly? NextReceiptDate,
    DateOnly? AvailableDate);

/// <summary>The stock position for one product and quantity. Figures only, no judgement (guardrail B2).</summary>
/// <param name="OfferingId">The product's id.</param>
/// <param name="Sku">The product's SKU.</param>
/// <param name="Name">The product's name, cleaned as untrusted text.</param>
/// <param name="Unit">The unit the quantities are in.</param>
/// <param name="Quantity">The quantity checked.</param>
/// <param name="Outlook">Whether all warehouses together can supply it now, later or at an unknown time.</param>
/// <param name="AvailableDate">The date that goes with <paramref name="Outlook"/>; null when unknown.</param>
/// <param name="TotalAvailable">Available across all warehouses.</param>
/// <param name="Warehouses">Each warehouse's figures, in code order.</param>
/// <param name="AsOf">When the figures were read (guardrail B1).</param>
public sealed record StockCheck(
    OfferingId OfferingId,
    Sku Sku,
    string Name,
    UnitOfMeasure Unit,
    decimal Quantity,
    StockOutlook Outlook,
    DateOnly? AvailableDate,
    decimal TotalAvailable,
    IReadOnlyList<WarehouseStock> Warehouses,
    DateTimeOffset AsOf);

/// <summary>Why a stock check was refused.</summary>
public static class CheckStockErrors
{
    /// <summary>The largest quantity a stock check accepts.</summary>
    public const decimal MaxQuantity = 1_000_000m;

    /// <summary>The offering is a service or plan, which has no stock.</summary>
    public static readonly DomainError OfferingNotStocked = new(
        "OfferingNotStocked", "That offering is a service or plan, so it has no stock. Only products can be checked.");

    /// <summary>The quantity is zero or negative.</summary>
    public static readonly DomainError QuantityNotPositive = new(
        Quantity.NotPositiveCode, "The quantity must be greater than zero.");

    /// <summary>The quantity is beyond any real order.</summary>
    public static readonly DomainError QuantityTooLarge = new(
        "QuantityTooLarge", "The quantity can be at most 1000000.");
}

/// <summary>
/// Checks whether a quantity of a product can ship, and when, from stock levels only. Dates are the
/// business's local days, the same days the seed's receipt dates use.
/// </summary>
/// <param name="offerings">Resolves the offering id or SKU.</param>
/// <param name="stock">Reads stock levels and warehouses.</param>
/// <param name="time">The clock for today and <see cref="StockCheck.AsOf"/>.</param>
public sealed class CheckStock(OfferingLookup offerings, IStockReader stock, TimeProvider time)
{
    /// <summary>Checks <paramref name="quantity"/> (1 if not given) of <paramref name="offering"/>.</summary>
    /// <param name="businessId">The session's business.</param>
    /// <param name="offering">An offering id or SKU, unchecked.</param>
    /// <param name="quantity">How many, in the product's unit; 1 if not given.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task<Result<StockCheck>> ExecuteAsync(
        BusinessId businessId,
        string? offering,
        decimal? quantity,
        CancellationToken cancellationToken)
    {
        var requested = quantity ?? 1m;
        if (requested <= 0m)
        {
            return Result.Failure<StockCheck>(CheckStockErrors.QuantityNotPositive);
        }

        if (requested > CheckStockErrors.MaxQuantity)
        {
            return Result.Failure<StockCheck>(CheckStockErrors.QuantityTooLarge);
        }

        var found = await offerings.FindAsync(businessId, offering, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return Result.Failure<StockCheck>(found.Error);
        }

        if (found.Value is not Product product)
        {
            return Result.Failure<StockCheck>(CheckStockErrors.OfferingNotStocked);
        }

        var levels = await stock.GetStockAsync(product.Id, cancellationToken).ConfigureAwait(false);
        var warehouses = (await stock.ListWarehousesAsync(businessId, cancellationToken).ConfigureAwait(false))
            .ToDictionary(w => w.Id);

        // One instant for both, so the date and asOf can't straddle midnight.
        var now = time.GetUtcNow();
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, time.LocalTimeZone).DateTime);
        var wanted = new Quantity(requested);

        var held = levels.Where(l => warehouses.ContainsKey(l.WarehouseId)).ToList();
        var lines = held
            .Select(l => (Level: l, Warehouse: warehouses[l.WarehouseId]))
            .OrderBy(x => x.Warehouse.Code, StringComparer.Ordinal)
            .Select(x => new WarehouseStock(
                x.Warehouse.Code,
                UntrustedText.Clean(x.Warehouse.Name, InventoryErrors.MaxNameLength),
                x.Level.Available,
                x.Level.OnOrder,
                x.Level.NextReceiptDate,
                x.Level.AvailableDate(wanted, today)))
            .ToList();

        var date = StockPosition.AvailableDate(held, wanted, today);
        var outlook = date is null ? StockOutlook.Unknown : date == today ? StockOutlook.Now : StockOutlook.Later;

        return Result.Success(new StockCheck(
            product.Id,
            product.Sku,
            UntrustedText.Clean(product.Name, CatalogErrors.MaxNameLength),
            product.Unit,
            requested,
            outlook,
            date,
            held.Sum(l => l.Available),
            lines,
            now));
    }
}
