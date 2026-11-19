using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Infrastructure.Persistence.Conversions;

/// <summary>
/// Stores a quote line's <see cref="PriceBreakdown"/> snapshot as JSON. A snapshot is never queried
/// field by field and must never change, so one text column keeps it whole and exactly as quoted.
/// The shape below is the stored format: change it only with a migration that rewrites old rows.
/// </summary>
internal sealed class PriceBreakdownConverter() : ValueConverter<PriceBreakdown, string>(
    v => SnapshotJson.Write(v),
    v => SnapshotJson.ReadPriceBreakdown(v));

/// <summary>Stores an order line's <see cref="VatLine"/> as JSON, for the same reason.</summary>
internal sealed class VatLineConverter() : ValueConverter<VatLine, string>(
    v => SnapshotJson.Write(v),
    v => SnapshotJson.ReadVatLine(v));

internal static class SnapshotJson
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Write(PriceBreakdown value) => JsonSerializer.Serialize(PriceBreakdownDto.From(value), _options);

    public static string Write(VatLine value) => JsonSerializer.Serialize(VatLineDto.From(value), _options);

    public static PriceBreakdown ReadPriceBreakdown(string json) =>
        (JsonSerializer.Deserialize<PriceBreakdownDto>(json, _options)
            ?? throw new JsonException("Empty price breakdown snapshot.")).ToDomain();

    public static VatLine ReadVatLine(string json) =>
        (JsonSerializer.Deserialize<VatLineDto>(json, _options)
            ?? throw new JsonException("Empty VAT line snapshot.")).ToDomain();

    private sealed record MoneyDto(decimal Amount, string Currency)
    {
        public static MoneyDto From(Money m) => new(m.Amount, m.Currency);

        public Money ToDomain() => new(Amount, Currency);
    }

    private sealed record VatLineDto(VatCode Code, decimal? Rate, MoneyDto Net, MoneyDto Vat, MoneyDto Total)
    {
        public static VatLineDto From(VatLine v) =>
            new(v.Code, v.Rate?.Value, MoneyDto.From(v.Net), MoneyDto.From(v.Vat), MoneyDto.From(v.Total));

        public VatLine ToDomain() =>
            new(Code, Rate is { } r ? new Percentage(r) : null, Net.ToDomain(), Vat.ToDomain(), Total.ToDomain());
    }

    private sealed record PriceStepDto(PriceStepKind Kind, string Detail, MoneyDto Amount);

    private sealed record PriceBreakdownDto(
        Guid OfferingId,
        Guid CustomerId,
        decimal Quantity,
        PriceStepKind Source,
        MoneyDto UnitPrice,
        MoneyDto Gross,
        decimal DiscountRate,
        MoneyDto Discount,
        VatLineDto Vat,
        DateTimeOffset AsOf,
        IReadOnlyList<PriceStepDto> Steps)
    {
        public static PriceBreakdownDto From(PriceBreakdown p) => new(
            p.OfferingId.Value,
            p.CustomerId.Value,
            p.Quantity.Value,
            p.Source,
            MoneyDto.From(p.UnitPrice),
            MoneyDto.From(p.Gross),
            p.DiscountRate.Value,
            MoneyDto.From(p.Discount),
            VatLineDto.From(p.Vat),
            p.AsOf,
            [.. p.Steps.Select(s => new PriceStepDto(s.Kind, s.Detail, MoneyDto.From(s.Amount)))]);

        public PriceBreakdown ToDomain() => new(
            new OfferingId(OfferingId),
            new CustomerId(CustomerId),
            new Quantity(Quantity),
            Source,
            UnitPrice.ToDomain(),
            Gross.ToDomain(),
            new Percentage(DiscountRate),
            Discount.ToDomain(),
            Vat.ToDomain(),
            AsOf,
            [.. Steps.Select(s => new PriceStep(s.Kind, s.Detail, s.Amount.ToDomain()))]);
    }
}
