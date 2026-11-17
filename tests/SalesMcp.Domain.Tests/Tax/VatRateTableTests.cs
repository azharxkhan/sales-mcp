using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Tests.Tax;

public class VatRateTableTests
{
    private static readonly VatRateTable _za = VatRateTable.SouthAfrica;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    [Fact]
    public void South_africa_defaults_are_15_0_and_exempt()
    {
        Assert.Equal(new Percentage(15m), _za.RateFor(VatCode.Standard));
        Assert.Equal(Percentage.Zero, _za.RateFor(VatCode.ZeroRated));
        Assert.Null(_za.RateFor(VatCode.Exempt));
    }

    [Fact]
    public void Three_lines_of_33_33_at_15_percent_round_per_line()
    {
        var line = _za.Calculate(Zar(33.33m) * 3m, VatCode.Standard);

        Assert.Equal(Zar(99.99m), line.Net);
        Assert.Equal(Zar(15.00m), line.Vat);
        Assert.Equal(Zar(114.99m), line.Total);
    }

    [Fact]
    public void Net_is_rounded_before_vat_is_calculated()
    {
        // Net 0.0951 rounds to 0.10, and 15% of 0.10 is 0.015 → 0.02.
        // VAT on the unrounded net would be 0.014265 → 0.01, and the line wouldn't add up as shown.
        var line = _za.Calculate(Zar(0.0951m), VatCode.Standard);

        Assert.Equal(Zar(0.10m), line.Net);
        Assert.Equal(Zar(0.02m), line.Vat);
        Assert.Equal(Zar(0.12m), line.Total);
    }

    [Fact]
    public void Zero_rated_lines_charge_vat_at_zero()
    {
        var line = _za.Calculate(Zar(250m), VatCode.ZeroRated);

        Assert.Equal(Zar(0m), line.Vat);
        Assert.Equal(Zar(250m), line.Total);
        Assert.Equal(Percentage.Zero, line.Rate);
        Assert.False(line.IsExempt);
    }

    [Fact]
    public void Exempt_lines_have_no_rate_and_no_vat()
    {
        var line = _za.Calculate(Zar(500m), VatCode.Exempt);

        Assert.Null(line.Rate);
        Assert.Equal(Zar(0m), line.Vat);
        Assert.Equal(Zar(500m), line.Total);
        Assert.True(line.IsExempt);
    }

    [Fact]
    public void Credit_lines_get_negative_vat()
    {
        var line = _za.Calculate(Zar(-100m), VatCode.Standard);

        Assert.Equal(Zar(-15m), line.Vat);
        Assert.Equal(Zar(-115m), line.Total);
    }

    [Fact]
    public void Configured_table_uses_the_configured_standard_rate()
    {
        var table = VatRateTable.Create(new Dictionary<VatCode, decimal> { [VatCode.Standard] = 16m }).Value;

        Assert.Equal(Zar(16m), table.Calculate(Zar(100m), VatCode.Standard).Vat);
    }

    [Fact]
    public void Configured_table_accepts_an_explicit_zero_rated_rate_of_zero() =>
        Assert.True(VatRateTable.Create(new Dictionary<VatCode, decimal>
        {
            [VatCode.Standard] = 15m,
            [VatCode.ZeroRated] = 0m,
        }).IsSuccess);

    public static TheoryData<Dictionary<VatCode, decimal>> InvalidTables => new()
    {
        new Dictionary<VatCode, decimal>(),
        new Dictionary<VatCode, decimal> { [VatCode.Standard] = 15m, [VatCode.ZeroRated] = 5m },
        new Dictionary<VatCode, decimal> { [VatCode.Standard] = 15m, [VatCode.Exempt] = 0m },
        new Dictionary<VatCode, decimal> { [VatCode.Standard] = 150m },
        new Dictionary<VatCode, decimal> { [VatCode.Standard] = -1m },
        new Dictionary<VatCode, decimal> { [VatCode.Standard] = 15m, [(VatCode)99] = 1m },
    };

    [Theory]
    [MemberData(nameof(InvalidTables))]
    public void Rejects_tables_that_break_the_rules(Dictionary<VatCode, decimal> rates)
    {
        var result = VatRateTable.Create(rates);

        Assert.True(result.IsFailure);
        Assert.Equal(VatRateTable.InvalidTableCode, result.Error.Code);
    }

    [Fact]
    public void Unknown_code_is_a_bug() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => _za.RateFor((VatCode)99));
}
