using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Tax;

/// <summary>
/// The VAT rate for each <see cref="VatCode"/>. Rates are data: build the table from configuration
/// with <see cref="Create"/>, or use <see cref="SouthAfrica"/> for the default.
/// </summary>
/// <remarks>
/// Rules: <see cref="VatCode.Standard"/> must have a rate; <see cref="VatCode.ZeroRated"/> is
/// always 0%; <see cref="VatCode.Exempt"/> never has a rate. Disclaimer: these are simplified
/// rules for a demo domain, not tax advice (ADR-0007).
/// </remarks>
public sealed class VatRateTable
{
    /// <summary>Error code for a table that breaks the rules above.</summary>
    public const string InvalidTableCode = "VatRateTableInvalid";

    private VatRateTable(Percentage standardRate) => StandardRate = standardRate;

    /// <summary>The standard rate.</summary>
    public Percentage StandardRate { get; }

    /// <summary>South Africa: standard 15%, zero-rated 0%, exempt.</summary>
    public static VatRateTable SouthAfrica { get; } = new(new Percentage(15m));

    /// <summary>
    /// Builds a table from configured rates, keyed by code. Exempt must be absent; zero-rated may be
    /// absent or 0.
    /// </summary>
    public static Result<VatRateTable> Create(IReadOnlyDictionary<VatCode, decimal> rates)
    {
        ArgumentNullException.ThrowIfNull(rates);

        if (!rates.TryGetValue(VatCode.Standard, out var standard))
        {
            return Invalid("The standard VAT rate is missing.");
        }

        if (rates.TryGetValue(VatCode.ZeroRated, out var zero) && zero != 0m)
        {
            return Invalid("The zero-rated VAT rate must be 0%.");
        }

        if (rates.ContainsKey(VatCode.Exempt))
        {
            return Invalid("Exempt items have no VAT rate, so none may be configured.");
        }

        if (rates.Keys.Any(code => !Enum.IsDefined(code)))
        {
            return Invalid("The table contains an unknown VAT code.");
        }

        return Percentage.Create(standard).Match(
            rate => Result.Success(new VatRateTable(rate)),
            _ => Invalid("The standard VAT rate must be between 0% and 100%."));
    }

    /// <summary>The rate for a code, or <see langword="null"/> for <see cref="VatCode.Exempt"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The code isn't a defined <see cref="VatCode"/>.</exception>
    public Percentage? RateFor(VatCode code) => code switch
    {
        VatCode.Standard => StandardRate,
        VatCode.ZeroRated => Percentage.Zero,
        VatCode.Exempt => null,
        _ => throw new ArgumentOutOfRangeException(nameof(code), code, "Unknown VAT code."),
    };

    /// <summary>
    /// VAT for one line. The net is rounded first, VAT is computed on the rounded net and rounded,
    /// and the total is their sum, so every line adds up exactly (ADR-0007: rounding is per line).
    /// </summary>
    /// <param name="net">The line amount after any discount.</param>
    /// <param name="code">The offering's VAT code.</param>
    public VatLine Calculate(Money net, VatCode code)
    {
        var roundedNet = net.Round();
        var rate = RateFor(code);
        var vat = rate is { } r ? r.Of(roundedNet).Round() : Money.Zero(net.Currency);
        return new VatLine(code, rate, roundedNet, vat, roundedNet + vat);
    }

    private static Result<VatRateTable> Invalid(string message) =>
        Result.Failure<VatRateTable>(InvalidTableCode, message);
}
