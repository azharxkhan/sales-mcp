using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tests.Common;

public class MoneyTests
{
    private static Money Zar(decimal amount) => new(amount, "ZAR");

    [Theory]
    [InlineData("")]
    [InlineData("zar")]
    [InlineData("ZA")]
    [InlineData("ZARR")]
    [InlineData("Z1R")]
    public void Rejects_currency_codes_that_are_not_three_upper_case_letters(string currency) =>
        Assert.Throws<ArgumentException>(() => new Money(1m, currency));

    [Fact]
    public void Adds_and_subtracts_in_the_same_currency()
    {
        Assert.Equal(Zar(15.50m), Zar(10m) + Zar(5.50m));
        Assert.Equal(Zar(4.50m), Zar(10m) - Zar(5.50m));
    }

    [Fact]
    public void Arithmetic_across_currencies_throws()
    {
        var usd = new Money(1m, "USD");

        Assert.Throws<InvalidOperationException>(() => Zar(1m) + usd);
        Assert.Throws<InvalidOperationException>(() => Zar(1m) - usd);
    }

    [Fact]
    public void Multiplies_by_a_factor_either_way_round()
    {
        Assert.Equal(Zar(99.99m), Zar(33.33m) * 3m);
        Assert.Equal(Zar(99.99m), 3m * Zar(33.33m));
    }

    [Theory]
    [InlineData("0.005", "0.01")]
    [InlineData("0.004", "0.00")]
    [InlineData("1.115", "1.12")]
    [InlineData("-0.005", "-0.01")]
    [InlineData("14.9985", "15.00")]
    public void Rounds_to_two_decimals_midpoint_away_from_zero(string input, string expected) =>
        Assert.Equal(
            Zar(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)),
            Zar(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)).Round());

    [Fact]
    public void Keeps_full_precision_until_rounded()
    {
        var vat = Zar(99.99m) * 0.15m;

        Assert.Equal(14.9985m, vat.Amount);
        Assert.Equal(15.00m, vat.Round().Amount);
    }

    [Fact]
    public void Sum_of_nothing_is_zero_in_the_given_currency() =>
        Assert.Equal(Money.Zero("ZAR"), Money.Sum([], "ZAR"));

    [Fact]
    public void Sum_adds_every_amount() =>
        Assert.Equal(Zar(6m), Money.Sum([Zar(1m), Zar(2m), Zar(3m)], "ZAR"));

    [Fact]
    public void Sum_refuses_amounts_in_another_currency() =>
        Assert.Throws<InvalidOperationException>(() => Money.Sum([Zar(1m), new Money(1m, "EUR")], "ZAR"));

    [Fact]
    public void Negation_and_sign_checks()
    {
        Assert.Equal(Zar(-5m), -Zar(5m));
        Assert.True(Zar(-0.01m).IsNegative);
        Assert.True(Money.Zero("ZAR").IsZero);
        Assert.False(Zar(0.01m).IsZero);
    }

    [Fact]
    public void Equal_amounts_in_the_same_currency_are_equal_regardless_of_trailing_zeros() =>
        Assert.Equal(Zar(1.5m), Zar(1.50m));

    [Fact]
    public void Same_amount_in_different_currencies_is_not_equal() =>
        Assert.NotEqual(Zar(1m), new Money(1m, "USD"));

    [Fact]
    public void Formats_invariantly_for_logs() =>
        Assert.Equal("ZAR 1234.50", Zar(1234.5m).ToString());
}
