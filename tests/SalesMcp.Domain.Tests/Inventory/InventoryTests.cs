using SalesMcp.Domain.Common;
using SalesMcp.Domain.Inventory;

namespace SalesMcp.Domain.Tests.Inventory;

public class WarehouseTests
{
    private static readonly BusinessId _acme = BusinessId.New();

    [Fact]
    public void Creates_a_warehouse_with_a_normalised_code()
    {
        var warehouse = Warehouse.Create(WarehouseId.New(), _acme, " dbn ", " Durban branch ").Value;

        Assert.Equal(_acme, warehouse.BusinessId);
        Assert.Equal("DBN", warehouse.Code);
        Assert.Equal("Durban branch", warehouse.Name);
    }

    [Theory]
    [InlineData(null, "Durban", "WarehouseCodeInvalid")]
    [InlineData("D", "Durban", "WarehouseCodeInvalid")]
    [InlineData("DURBAN-NTH", "Durban", "WarehouseCodeInvalid")]
    [InlineData("DURBANNORTH", "Durban", "WarehouseCodeInvalid")]
    [InlineData("DBN", " ", "NameRequired")]
    public void Rejects_invalid_input(string? code, string? name, string expectedCode) =>
        Assert.Equal(expectedCode, Warehouse.Create(WarehouseId.New(), _acme, code, name).Error.Code);

    [Fact]
    public void Accepts_codes_at_the_length_limits()
    {
        Assert.True(Warehouse.Create(WarehouseId.New(), _acme, "JH", "Johannesburg").IsSuccess);
        Assert.True(Warehouse.Create(WarehouseId.New(), _acme, "DURBANNRTH", "Durban North").IsSuccess);
    }

    [Fact]
    public void Rejects_a_long_name() =>
        Assert.Equal(
            InventoryErrors.NameTooLong,
            Warehouse.Create(WarehouseId.New(), _acme, "DBN", new string('n', 201)).Error);
}

public class StockLevelTests
{
    private static readonly DateOnly _today = new(2026, 11, 18);
    private static readonly DateOnly _receipt = new(2026, 11, 25);

    private static StockLevel Stock(decimal onHand, decimal allocated = 0m, decimal onOrder = 0m, DateOnly? receipt = null) =>
        StockLevel.Create(OfferingId.New(), WarehouseId.New(), onHand, allocated, onOrder, receipt).Value;

    [Fact]
    public void Creates_a_stock_level_with_its_fields()
    {
        var offering = OfferingId.New();
        var warehouse = WarehouseId.New();
        var stock = StockLevel.Create(offering, warehouse, 120m, 20m, 200m, _receipt).Value;

        Assert.Equal(offering, stock.OfferingId);
        Assert.Equal(warehouse, stock.WarehouseId);
        Assert.Equal(120m, stock.OnHand);
        Assert.Equal(20m, stock.Allocated);
        Assert.Equal(200m, stock.OnOrder);
        Assert.Equal(_receipt, stock.NextReceiptDate);
    }

    [Theory]
    [InlineData(120, 20, 100)]
    [InlineData(50, 50, 0)]
    [InlineData(0, 0, 0)]
    public void Available_is_on_hand_less_allocated(decimal onHand, decimal allocated, decimal expected) =>
        Assert.Equal(expected, Stock(onHand, allocated).Available);

    [Theory]
    [InlineData(-1, 0, 0, "QuantityNegative")]
    [InlineData(10, -1, 0, "QuantityNegative")]
    [InlineData(10, 0, -1, "QuantityNegative")]
    [InlineData(10, 11, 0, "OverAllocated")]
    public void Rejects_invalid_quantities(decimal onHand, decimal allocated, decimal onOrder, string expectedCode) =>
        Assert.Equal(
            expectedCode,
            StockLevel.Create(OfferingId.New(), WarehouseId.New(), onHand, allocated, onOrder, null).Error.Code);

    [Fact]
    public void Rejects_a_receipt_date_with_nothing_on_order() =>
        Assert.Equal(
            InventoryErrors.ReceiptWithoutOrder,
            StockLevel.Create(OfferingId.New(), WarehouseId.New(), 10m, 0m, 0m, _receipt).Error);

    [Fact]
    public void Allows_stock_on_order_with_no_known_receipt_date() =>
        Assert.Null(Stock(0m, onOrder: 50m).NextReceiptDate);

    // On hand 100, allocated 40 (60 available), 200 on order due on the 25th.
    [Theory]
    [InlineData(1, "2026-11-18")]
    [InlineData(60, "2026-11-18")]
    [InlineData(60.5, "2026-11-25")]
    [InlineData(260, "2026-11-25")]
    [InlineData(260.01, null)]
    public void Available_date_is_today_then_the_receipt_date_then_unknown(double quantity, string? expected)
    {
        var stock = Stock(100m, allocated: 40m, onOrder: 200m, receipt: _receipt);

        var date = stock.AvailableDate(new Quantity((decimal)quantity), _today);

        Assert.Equal(expected is null ? null : DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), date);
    }

    [Fact]
    public void Unknown_when_on_order_has_no_receipt_date() =>
        Assert.Null(Stock(10m, onOrder: 100m).AvailableDate(new Quantity(50), _today));

    [Fact]
    public void Unknown_when_nothing_is_on_order() =>
        Assert.Null(Stock(10m).AvailableDate(new Quantity(11), _today));

    [Fact]
    public void Unknown_when_the_receipt_date_has_passed()
    {
        var late = Stock(0m, onOrder: 100m, receipt: _today.AddDays(-1));

        Assert.Null(late.AvailableDate(new Quantity(5), _today));
    }

    [Fact]
    public void A_receipt_due_today_counts() =>
        Assert.Equal(_today, Stock(0m, onOrder: 100m, receipt: _today).AvailableDate(new Quantity(5), _today));
}
