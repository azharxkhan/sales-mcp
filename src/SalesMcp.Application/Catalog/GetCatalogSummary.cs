using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;

namespace SalesMcp.Application.Catalog;

/// <summary>One category and how many offerings are on sale in it.</summary>
/// <param name="Name">The category, cleaned as untrusted text.</param>
/// <param name="Count">Active offerings in it.</param>
public sealed record CategoryCount(string Name, int Count);

/// <summary>What a business sells, in numbers rather than as a list.</summary>
/// <param name="BusinessId">The business.</param>
/// <param name="BusinessName">Its name, cleaned.</param>
/// <param name="Total">Active offerings.</param>
/// <param name="Products">Of which products.</param>
/// <param name="Services">Of which services.</param>
/// <param name="Plans">Of which plans.</param>
/// <param name="Categories">Each category with its count, by name.</param>
public sealed record CatalogSummary(
    BusinessId BusinessId,
    string BusinessName,
    int Total,
    int Products,
    int Services,
    int Plans,
    IReadOnlyList<CategoryCount> Categories);

/// <summary>Errors from <see cref="GetCatalogSummary"/>.</summary>
public static class CatalogSummaryErrors
{
    /// <summary>The catalog asked for isn't this session's business's.</summary>
    public static readonly DomainError CatalogNotFound = new("CatalogNotFound", "There is no such catalog.");
}

/// <summary>Counts the business's active offerings by kind and category.</summary>
/// <param name="businesses">Reads the business.</param>
/// <param name="catalog">Reads the catalog.</param>
public sealed class GetCatalogSummary(IBusinessReader businesses, ICatalogReader catalog)
{
    /// <summary>The name offerings without a category are counted under.</summary>
    public const string Uncategorised = "Uncategorised";

    /// <summary>Summarises the catalog of <paramref name="businessId"/>, the session's business.</summary>
    public async Task<Result<CatalogSummary>> ExecuteAsync(BusinessId businessId, CancellationToken cancellationToken)
    {
        var business = await businesses.GetBusinessAsync(businessId, cancellationToken).ConfigureAwait(false);
        if (business is null)
        {
            return Result.Failure<CatalogSummary>(CatalogSummaryErrors.CatalogNotFound);
        }

        var offerings = await catalog.ListActiveOfferingsAsync(businessId, cancellationToken).ConfigureAwait(false);
        var categories = offerings
            .GroupBy(CategoryOf, StringComparer.Ordinal)
            .Select(g => new CategoryCount(g.Key, g.Count()))
            .OrderBy(c => c.Name, StringComparer.Ordinal)
            .ToList();

        return Result.Success(new CatalogSummary(
            businessId,
            UntrustedText.Clean(business.Name, CatalogErrors.MaxNameLength),
            offerings.Count,
            offerings.Count(o => o.Kind == OfferingKind.Product),
            offerings.Count(o => o.Kind == OfferingKind.Service),
            offerings.Count(o => o.Kind == OfferingKind.Plan),
            categories));
    }

    private static string CategoryOf(Offering offering)
    {
        var name = UntrustedText.Clean(offering.Category, CatalogErrors.MaxCategoryLength);
        return name.Length == 0 ? Uncategorised : name;
    }
}
