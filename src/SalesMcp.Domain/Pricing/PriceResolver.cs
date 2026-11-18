using System.Globalization;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Pricing;

/// <summary>What to price.</summary>
/// <param name="Offering">The offering. Must be active.</param>
/// <param name="Customer">The buyer.</param>
/// <param name="Quantity">How many, in the offering's unit.</param>
/// <param name="DiscountRate">The discount already approved for this line. Caps and approvals are checked before this (M4).</param>
/// <param name="AsOf">The moment to price at, from the caller's clock. Contract validity uses its local date.</param>
public sealed record PriceRequest(
    Offering Offering,
    Customer Customer,
    Quantity Quantity,
    Percentage DiscountRate,
    DateTimeOffset AsOf);

/// <summary>Where prices can come from for one customer.</summary>
/// <param name="DefaultList">The business's default price list.</param>
/// <param name="CustomerList">The customer's own list, or null. Must be the list assigned to the customer.</param>
/// <param name="Contracts">Contract prices to consider. Ones for other customers or offerings are ignored.</param>
public sealed record PriceSources(
    PriceList DefaultList,
    PriceList? CustomerList,
    IReadOnlyCollection<ContractPrice> Contracts);

/// <summary>
/// The only source of prices (guardrail A1). Picks the unit price, applies the discount, then VAT
/// per line, then rounding, and records each step in a <see cref="PriceBreakdown"/>.
/// </summary>
/// <remarks>
/// Order of precedence (a simplified SAP condition technique):
/// <list type="number">
/// <item>a contract for this customer and offering valid on the pricing date (the lowest, if several);</item>
/// <item>the customer's price list, best quantity break;</item>
/// <item>the default price list, best quantity break;</item>
/// <item>otherwise <see cref="PricingErrors.NoPrice"/>. A missing price is never treated as zero.</item>
/// </list>
/// </remarks>
public sealed class PriceResolver(VatRateTable vatRates)
{
    private readonly VatRateTable _vatRates = vatRates ?? throw new ArgumentNullException(nameof(vatRates));

    /// <summary>Prices one line, or explains why it can't be priced.</summary>
    public Result<PriceBreakdown> Resolve(PriceRequest request, PriceSources sources)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(sources);

        var check = CheckInputs(request, sources);
        if (check.IsFailure)
        {
            return Result.Failure<PriceBreakdown>(check.Error);
        }

        var unit = ChooseUnitPrice(request, sources);
        if (unit is null)
        {
            return Result.Failure<PriceBreakdown>(PricingErrors.NoPrice);
        }

        var (source, unitPrice, detail) = unit.Value;
        var steps = new List<PriceStep> { new(source, detail, unitPrice) };

        var gross = (unitPrice * request.Quantity.Value).Round();
        var discount = request.DiscountRate.Of(gross).Round();
        if (!discount.IsZero)
        {
            steps.Add(new PriceStep(PriceStepKind.Discount, $"{request.DiscountRate} off", -discount));
        }

        var vat = _vatRates.Calculate(gross - discount, request.Offering.VatCode);
        steps.Add(new PriceStep(PriceStepKind.Vat, VatDetail(vat), vat.Vat));

        return Result.Success(new PriceBreakdown(
            request.Offering.Id,
            request.Customer.Id,
            request.Quantity,
            source,
            unitPrice,
            gross,
            request.DiscountRate,
            discount,
            vat,
            request.AsOf,
            steps));
    }

    private static Result CheckInputs(PriceRequest request, PriceSources sources)
    {
        if (!request.Offering.IsActive)
        {
            return Result.Failure(PricingErrors.OfferingInactive);
        }

        var business = request.Offering.BusinessId;
        if (request.Customer.BusinessId != business
            || sources.DefaultList.BusinessId != business
            || (sources.CustomerList is { } own && own.BusinessId != business))
        {
            return Result.Failure(PricingErrors.BusinessMismatch);
        }

        // The customer list must be exactly the one assigned, so one customer's prices can't be
        // used for another (guardrail: customer bound to the session).
        if (sources.CustomerList?.Id != request.Customer.PriceListId)
        {
            return Result.Failure(PricingErrors.PriceListMismatch);
        }

        // A contract in another currency is an error, not something to skip: skipping it would
        // quietly charge the list price instead of the agreed one.
        var currency = sources.DefaultList.Currency;
        if ((sources.CustomerList is { } list && list.Currency != currency)
            || sources.Contracts.Any(c => c.CustomerId == request.Customer.Id
                && c.OfferingId == request.Offering.Id
                && c.UnitPrice.Currency != currency))
        {
            return Result.Failure(PricingErrors.CurrencyMismatch);
        }

        return Result.Success();
    }

    private static (PriceStepKind Source, Money UnitPrice, string Detail)? ChooseUnitPrice(
        PriceRequest request,
        PriceSources sources)
    {
        var offeringId = request.Offering.Id;
        var date = DateOnly.FromDateTime(request.AsOf.DateTime);

        var contract = sources.Contracts
            .Where(c => c.CustomerId == request.Customer.Id && c.OfferingId == offeringId && c.IsValidOn(date))
            .MinBy(c => c.UnitPrice.Amount);
        if (contract is not null)
        {
            var range = string.Create(
                CultureInfo.InvariantCulture,
                $"Contract price, valid {contract.ValidFrom:yyyy-MM-dd} to {contract.ValidTo:yyyy-MM-dd}");
            return (PriceStepKind.ContractPrice, contract.UnitPrice, range);
        }

        if (sources.CustomerList?.BestBreak(offeringId, request.Quantity) is { } own)
        {
            return (PriceStepKind.CustomerPriceList, own.UnitPrice, BreakDetail(sources.CustomerList, own));
        }

        if (sources.DefaultList.BestBreak(offeringId, request.Quantity) is { } listed)
        {
            return (PriceStepKind.DefaultPriceList, listed.UnitPrice, BreakDetail(sources.DefaultList, listed));
        }

        return null;
    }

    private static string BreakDetail(PriceList list, PriceListItem item) => $"{list.Name}, {item.MinQuantity}+ break";

    private static string VatDetail(VatLine vat) => vat.Code switch
    {
        VatCode.Exempt => "VAT exempt",
        VatCode.ZeroRated => "VAT zero-rated (0%)",
        _ => $"VAT {vat.Rate}",
    };
}
