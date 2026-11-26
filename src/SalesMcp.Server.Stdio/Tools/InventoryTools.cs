using System.ComponentModel;
using ModelContextProtocol.Server;
using SalesMcp.Application.Inventory;
using SalesMcp.Application.Session;

namespace SalesMcp.Server.Stdio.Tools;

/// <summary>Stock tools. Thin adapters over the Application use cases.</summary>
/// <param name="checkStock">The stock use case.</param>
/// <param name="session">The bound business and customer.</param>
[McpServerToolType]
public sealed class InventoryTools(CheckStock checkStock, ISessionContext session)
{
    /// <summary>The <c>check_stock</c> tool.</summary>
    [McpServerTool(Name = "check_stock", Title = "Check stock", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Returns whether a quantity of a product can ship now, by a known date, or at an unknown time, with the available and on-order figures for each warehouse and the time they were read.
        Use it before promising availability or a delivery date, or when the buyer asks what is in stock.
        It works for products only, not services or plans, and never returns prices. Report the figures and dates as given: never add urgency or scarcity, and when availability is "unknown", say nobody can promise a date yet.
        """)]
    public async Task<StockResult> CheckStockAsync(
        [Description("The product's id or SKU from search_catalog, for example \"ACM-PPE-101\".")]
        string offering,
        [Description("Optional: how many, in the product's unit, greater than 0 and at most 1000000, for example 24. Defaults to 1.")]
        decimal? quantity = null,
        CancellationToken cancellationToken = default)
    {
        var result = await checkStock
            .ExecuteAsync(session.BusinessId, offering, quantity, cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess ? StockResult.From(result.Value) : throw ToolError.From(result.Error);
    }
}

/// <summary>The structured result of <c>check_stock</c>.</summary>
/// <param name="OfferingId">The product's id.</param>
/// <param name="Sku">The product's SKU.</param>
/// <param name="Name">The product's name.</param>
/// <param name="Unit">The unit the quantities are in, such as <c>each</c> or <c>box</c>.</param>
/// <param name="Quantity">The quantity checked.</param>
/// <param name="Availability"><c>now</c>, <c>later</c> (on <c>availableDate</c>) or <c>unknown</c>, for all warehouses together.</param>
/// <param name="AvailableDate">When the quantity can be supplied (yyyy-MM-dd); null when unknown.</param>
/// <param name="TotalAvailable">Available now across all warehouses.</param>
/// <param name="Warehouses">Each warehouse's figures, in code order.</param>
/// <param name="AsOf">When the figures were read (UTC).</param>
public sealed record StockResult(
    string OfferingId,
    string Sku,
    string Name,
    string Unit,
    decimal Quantity,
    string Availability,
    DateOnly? AvailableDate,
    decimal TotalAvailable,
    IReadOnlyList<WarehouseStockResult> Warehouses,
    DateTimeOffset AsOf)
{
    /// <summary>Maps the use case's stock check.</summary>
    public static StockResult From(StockCheck check)
    {
        ArgumentNullException.ThrowIfNull(check);
        return new StockResult(
            check.OfferingId.ToString(),
            check.Sku.Value,
            check.Name,
            Wire.Name(check.Unit),
            check.Quantity,
            Wire.Name(check.Outlook),
            check.AvailableDate,
            check.TotalAvailable,
            [.. check.Warehouses.Select(WarehouseStockResult.From)],
            check.AsOf);
    }
}

/// <summary>One warehouse in a <c>check_stock</c> result.</summary>
/// <param name="Code">The warehouse code, for example <c>JHB</c>.</param>
/// <param name="Name">The warehouse name.</param>
/// <param name="Available">On hand less what is promised to other orders.</param>
/// <param name="OnOrder">Due in on open purchase orders.</param>
/// <param name="NextReceiptDate">When the next purchase order is due (yyyy-MM-dd), or null if none or unknown.</param>
/// <param name="AvailableDate">When this warehouse alone can supply the quantity (yyyy-MM-dd), or null if unknown.</param>
public sealed record WarehouseStockResult(
    string Code,
    string Name,
    decimal Available,
    decimal OnOrder,
    DateOnly? NextReceiptDate,
    DateOnly? AvailableDate)
{
    /// <summary>Maps one warehouse's figures.</summary>
    public static WarehouseStockResult From(WarehouseStock stock)
    {
        ArgumentNullException.ThrowIfNull(stock);
        return new WarehouseStockResult(stock.Code, stock.Name, stock.Available, stock.OnOrder, stock.NextReceiptDate, stock.AvailableDate);
    }
}
