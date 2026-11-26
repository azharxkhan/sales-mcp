using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;

namespace SalesMcp.Application.Catalog;

/// <summary>Why an offering a tool was given couldn't be used.</summary>
public static class OfferingLookupErrors
{
    /// <summary>No offering was given.</summary>
    public static readonly DomainError OfferingRequired = new(
        "OfferingRequired", "Give the offering's id or SKU, for example ACM-BRG-6204. Search the catalog to find it.");

    /// <summary>No offering in this business's catalog has that id or SKU.</summary>
    public static readonly DomainError OfferingNotFound = new(
        "OfferingNotFound", "No offering in this catalog has that id or SKU. Search the catalog to find the right one.");

    /// <summary>The offering exists but is no longer sold.</summary>
    public static readonly DomainError OfferingInactive = new(
        "OfferingInactive", "That offering is no longer sold. Search the catalog for an alternative.");
}

/// <summary>
/// Finds the offering a tool was given, as an id or a SKU, in the session's business. Every tool that
/// takes an offering uses it, so they all accept and refuse the same things (guardrail D7).
/// </summary>
/// <param name="catalog">Reads the catalog.</param>
public sealed class OfferingLookup(ICatalogReader catalog)
{
    /// <summary>
    /// The active offering with this id or SKU in <paramref name="businessId"/>. Another business's
    /// offering is "not found", so ids can't be probed.
    /// </summary>
    /// <param name="businessId">The session's business.</param>
    /// <param name="reference">An offering id (a GUID) or a SKU, unchecked.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public async Task<Result<Offering>> FindAsync(BusinessId businessId, string? reference, CancellationToken cancellationToken)
    {
        var text = reference?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return Result.Failure<Offering>(OfferingLookupErrors.OfferingRequired);
        }

        Offering? offering = null;
        if (Guid.TryParse(text, out var id))
        {
            offering = await catalog.GetOfferingAsync(new OfferingId(id), cancellationToken).ConfigureAwait(false);
        }
        else if (Sku.Create(text) is { IsSuccess: true } sku)
        {
            offering = await catalog.FindBySkuAsync(businessId, sku.Value, cancellationToken).ConfigureAwait(false);
        }

        if (offering is null || offering.BusinessId != businessId)
        {
            return Result.Failure<Offering>(OfferingLookupErrors.OfferingNotFound);
        }

        return offering.IsActive
            ? Result.Success(offering)
            : Result.Failure<Offering>(OfferingLookupErrors.OfferingInactive);
    }
}
