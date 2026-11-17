using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Tests.Tax;

public class VatSummaryTests
{
    private static readonly VatRateTable _za = VatRateTable.SouthAfrica;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    [Fact]
    public void Mixed_basket_shows_vat_per_code_and_adds_up()
    {
        VatLine[] lines =
        [
            _za.Calculate(Zar(33.33m) * 3m, VatCode.Standard), // 99.99 + 15.00
            _za.Calculate(Zar(200m), VatCode.Standard),        // 200.00 + 30.00
            _za.Calculate(Zar(80m), VatCode.ZeroRated),        // 80.00 + 0.00
            _za.Calculate(Zar(50m), VatCode.Exempt),           // 50.00, exempt
        ];

        var summary = VatSummary.Of(lines, "ZAR");

        Assert.Collection(
            summary.ByCode,
            standard =>
            {
                Assert.Equal(VatCode.Standard, standard.Code);
                Assert.Equal(Zar(299.99m), standard.Net);
                Assert.Equal(Zar(45.00m), standard.Vat);
            },
            zero =>
            {
                Assert.Equal(VatCode.ZeroRated, zero.Code);
                Assert.Equal(Zar(80m), zero.Net);
                Assert.Equal(Zar(0m), zero.Vat);
            },
            exempt =>
            {
                Assert.Equal(VatCode.Exempt, exempt.Code);
                Assert.Null(exempt.Rate);
                Assert.Equal(Zar(50m), exempt.Net);
            });

        Assert.Equal(Zar(429.99m), summary.Net);
        Assert.Equal(Zar(45.00m), summary.Vat);
        Assert.Equal(Zar(474.99m), summary.Total);
        Assert.Equal(Money.Sum(lines.Select(l => l.Total), "ZAR"), summary.Total);
    }

    [Fact]
    public void Totals_are_the_sum_of_rounded_lines_not_vat_on_the_total()
    {
        // Three lines of R0.10: VAT per line is 0.015 → 0.02, so 0.06 in total.
        // VAT on the summed net (0.30) would be 0.045 → 0.05. The document must use the per-line figure.
        var lines = Enumerable.Range(0, 3).Select(_ => _za.Calculate(Zar(0.10m), VatCode.Standard)).ToList();

        Assert.Equal(Zar(0.06m), VatSummary.Of(lines, "ZAR").Vat);
    }

    [Fact]
    public void Empty_document_is_all_zero()
    {
        var summary = VatSummary.Of([], "ZAR");

        Assert.Empty(summary.ByCode);
        Assert.Equal(Money.Zero("ZAR"), summary.Total);
    }

    [Fact]
    public void Lines_in_another_currency_are_a_bug() =>
        Assert.Throws<InvalidOperationException>(() =>
            VatSummary.Of([_za.Calculate(new Money(10m, "USD"), VatCode.Standard)], "ZAR"));
}
