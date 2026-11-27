using SalesMcp.Domain.Common;
using SalesMcp.Domain.Inventory;

namespace SalesMcp.Domain.Tests.Inventory;

public class StockPositionTests
{
    private static readonly DateOnly _today = new(2026, 11, 26);
    private static readonly OfferingId _product = OfferingId.New();

    private static StockLevel Level(decimal onHand, decimal allocated = 0m, decimal onOrder = 0m, DateOnly? receipt = null) =>
        StockLevel.Create(_product, WarehouseId.New(), onHand, allocated, onOrder, receipt).Value;

    private static DateOnly? DateFor(decimal quantity, params StockLevel[] levels) =>
        StockPosition.AvailableDate(levels, new Quantity(quantity), _today);

    [Fact]
    public void Stock_in_two_warehouses_together_covers_today() =>
        Assert.Equal(_today, DateFor(8m, Level(5m), Level(5m)));

    [Fact]
    public void Allocated_stock_is_not_available() =>
        Assert.Null(DateFor(8m, Level(10m, allocated: 4m)));

    [Fact]
    public void The_first_receipt_that_covers_the_shortfall_gives_the_date()
    {
        var early = _today.AddDays(5);
        var late = _today.AddDays(12);

        Assert.Equal(early, DateFor(12m, Level(4m, onOrder: 10m, receipt: early), Level(4m, onOrder: 50m, receipt: late)));
        Assert.Equal(late, DateFor(30m, Level(4m, onOrder: 10m, receipt: early), Level(4m, onOrder: 50m, receipt: late)));
    }

    [Fact]
    public void Too_little_even_with_every_receipt_is_unknown() =>
        Assert.Null(DateFor(100m, Level(4m, onOrder: 10m, receipt: _today.AddDays(5))));

    [Fact]
    public void A_late_purchase_order_is_not_counted() =>
        Assert.Null(DateFor(10m, Level(4m, onOrder: 10m, receipt: _today.AddDays(-1))));

    [Fact]
    public void No_stock_records_is_unknown() =>
        Assert.Null(DateFor(1m));
}
