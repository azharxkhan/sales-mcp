using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tests.Common;

public class PercentageTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(100)]
    public void Accepts_values_from_0_to_100(int value)
    {
        Assert.Equal(value, new Percentage(value).Value);
        Assert.True(Percentage.Create(value).IsSuccess);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Constructor_throws_outside_0_to_100(double value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Percentage((decimal)value));

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Create_returns_a_failure_outside_0_to_100(int value)
    {
        var result = Percentage.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(Percentage.OutOfRangeCode, result.Error.Code);
    }

    [Fact]
    public void Of_returns_the_unrounded_share_of_an_amount()
    {
        var amount = new Money(99.99m, "ZAR");

        Assert.Equal(new Money(14.9985m, "ZAR"), new Percentage(15m).Of(amount));
    }

    [Fact]
    public void Compares_by_value()
    {
        Assert.True(new Percentage(5m) < new Percentage(10m));
        Assert.True(new Percentage(10m) >= new Percentage(10m));
        Assert.Equal(0.1m, new Percentage(10m).AsFraction);
    }

    [Fact]
    public void Formats_invariantly() =>
        Assert.Equal("12.5%", new Percentage(12.5m).ToString());
}

public class QuantityTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_throws_for_zero_or_negative(int value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(value));

    [Fact]
    public void Create_returns_a_failure_for_zero()
    {
        var result = Quantity.Create(0m);

        Assert.True(result.IsFailure);
        Assert.Equal(Quantity.NotPositiveCode, result.Error.Code);
    }

    [Fact]
    public void Allows_fractional_quantities_for_units_such_as_metres() =>
        Assert.Equal(2.5m, Quantity.Create(2.5m).Value.Value);

    [Fact]
    public void Compares_by_value() =>
        Assert.True(new Quantity(9m) < new Quantity(10m));
}
