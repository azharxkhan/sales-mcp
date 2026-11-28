using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;

namespace SalesMcp.Application.Catalog;

/// <summary>One offering in full, without a price: prices come only from the price resolver.</summary>
/// <param name="Offering">The offering.</param>
/// <param name="Name">Its name, cleaned as untrusted text.</param>
/// <param name="Category">Its category, cleaned; empty when it has none.</param>
/// <param name="DescriptionText">Its description, cleaned as untrusted text.</param>
public sealed record OfferingDetails(Offering Offering, string Name, string Category, string DescriptionText);

/// <summary>Reads one active offering of the session's business.</summary>
/// <param name="offerings">Resolves the offering id or SKU.</param>
public sealed class GetOfferingDetails(OfferingLookup offerings)
{
    /// <summary>The longest description returned.</summary>
    public const int DescriptionLength = 2_000;

    /// <summary>The details of <paramref name="offering"/>, an id or SKU.</summary>
    public async Task<Result<OfferingDetails>> ExecuteAsync(BusinessId businessId, string? offering, CancellationToken cancellationToken)
    {
        var found = await offerings.FindAsync(businessId, offering, cancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return Result.Failure<OfferingDetails>(found.Error);
        }

        var item = found.Value;
        return Result.Success(new OfferingDetails(
            item,
            UntrustedText.Clean(item.Name, CatalogErrors.MaxNameLength),
            UntrustedText.Clean(item.Category, CatalogErrors.MaxCategoryLength),
            UntrustedText.Clean(item.Description, DescriptionLength)));
    }
}
