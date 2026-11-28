using SalesMcp.Domain.Orders;

namespace SalesMcp.Domain.Tests.Orders;

public class OrderNumberTests
{
    [Fact]
    public void Formats_year_and_sequence() =>
        Assert.Equal("SO-2026-000123", OrderNumber.From(2026, 123).Value.ToString());

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 1_000_000)]
    public void Rejects_years_and_sequences_out_of_range(int year, int sequence) =>
        Assert.Equal(OrderErrors.NumberInvalid, OrderNumber.From(year, sequence).Error);

    [Theory]
    [InlineData("SO-2026-000123")]
    [InlineData(" so-2026-000123 ")]
    public void Parses_ignoring_case_and_spaces(string input) =>
        Assert.Equal(OrderNumber.From(2026, 123).Value, OrderNumber.Parse(input).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Q-2026-000123")]
    [InlineData("SO-2026-00012")]
    [InlineData("SO-2026-0001234")]
    [InlineData("SO-20X6-000123")]
    [InlineData("SO-2026_000123")]
    [InlineData("SO-2026-000000")]
    public void Rejects_anything_else(string? input) =>
        Assert.Equal(OrderErrors.NumberInvalid, OrderNumber.Parse(input).Error);
}
