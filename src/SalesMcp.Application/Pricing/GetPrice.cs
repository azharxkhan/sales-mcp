using SalesMcp.Application.Catalog;
using SalesMcp.Application.Customers;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Application.Pricing;

/// <summary>A price for the session's customer, with the offering it is for.</summary>
/// <param name="Sku">The offering's SKU.</param>
/// <param name="Name">The offering's name, cleaned as untrusted text.</param>
/// <param name="Unit">The unit the quantity is in; products only.</param>
/// <param name="Breakdown">The resolver's breakdown: the only source of the figures (guardrail A1).</param>
/// <param name="Steps">The breakdown's steps, with each detail cleaned as untrusted text.</param>
public sealed record PricedLine(
    Sku Sku,
    string Name,
    UnitOfMeasure? Unit,
    PriceBreakdown Breakdown,
    IReadOnlyList<PriceStep> Steps);

/// <summary>
/// Prices an offering for the session's customer through the <see cref="PriceResolver"/>, the only
/// source of prices (guardrail A1). No discount: discounts need approval and arrive in M4.
/// </summary>
/// <param name="offerings">Resolves the offering id or SKU.</param>
/// <param name="customers">Reads the session's customer.</param>
/// <param name="pricing">Loads the customer's price lists and contracts.</param>
/// <param name="resolver">Works out the price.</param>
/// <param name="time">The clock for the pricing moment.</param>
public sealed class GetPrice(
    OfferingLookup offerings,
    ICustomerReader customers,
    IPricingReader pricing,
    PriceResolver resolver,
    TimeProvider time)
{
    /// <summary>Longest step detail returned.</summary>
    public const int MaxDetailLength = 300;

    /// <summary>Prices <paramref name="quantity"/> of <paramref name="offering"/> for <paramref name="customerId"/>.</summary>
    /// <param name="businessId">The session's business.</param>
    /// <param name="customerId">The session's customer. Never a model argument (guardrail D2).</param>
    /// <param name="offering">An offering id or SKU, unchecked.</param>
    /// <param name="quantity">How many, in the offering's unit, unchecked.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    public async Task<Result<PricedLine>> ExecuteAsync(
        BusinessId businessId,
        CustomerId customerId,
        string? offering,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        var wanted = QuantityInput.Validate(quantity);
        if (wanted.IsFailure)
        {
            return Result.Failure<PricedLine>(wanted.Error);
        }

        var found = await offerings.FindAsync(businessId, offering, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return Result.Failure<PricedLine>(found.Error);
        }

        var customer = await customers.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null || customer.BusinessId != businessId)
        {
            return Result.Failure<PricedLine>(CustomerQueryErrors.CustomerNotFound);
        }

        var sources = await pricing.GetPriceSourcesAsync(customer, cancellationToken).ConfigureAwait(false);
        if (sources is null)
        {
            return Result.Failure<PricedLine>(PricingErrors.NoPrice);
        }

        // The resolver reads contract validity from the moment's own date, so give it the local day.
        var asOf = TimeZoneInfo.ConvertTime(time.GetUtcNow(), time.LocalTimeZone);
        var request = new PriceRequest(found.Value, customer, wanted.Value, Percentage.Zero, asOf);
        var priced = resolver.Resolve(request, sources);
        if (priced.IsFailure)
        {
            return Result.Failure<PricedLine>(priced.Error);
        }

        var item = found.Value;
        return Result.Success(new PricedLine(
            item.Sku,
            UntrustedText.Clean(item.Name, CatalogErrors.MaxNameLength),
            (item as Product)?.Unit,
            priced.Value,
            [.. priced.Value.Steps.Select(s => s with { Detail = UntrustedText.Clean(s.Detail, MaxDetailLength) })]));
    }
}
