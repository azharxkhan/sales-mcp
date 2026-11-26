using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;

namespace SalesMcp.Application.Catalog;

/// <summary>A catalog search, as a tool receives it: every field unchecked.</summary>
/// <param name="Query">Words to find in the SKU, name or description.</param>
/// <param name="Kind">Optional filter: <c>product</c>, <c>service</c> or <c>plan</c>.</param>
/// <param name="Limit">Results per page; <see cref="SearchCatalog.DefaultLimit"/> if not given.</param>
/// <param name="Cursor">The <see cref="CatalogPage.NextCursor"/> of the previous page, to continue.</param>
public sealed record CatalogSearch(string? Query, string? Kind = null, int? Limit = null, string? Cursor = null);

/// <summary>One offering in search results. No price: prices come only from the price resolver.</summary>
/// <param name="Id">The offering's id.</param>
/// <param name="Sku">Its SKU.</param>
/// <param name="Name">Its name.</param>
/// <param name="Kind">Product, service or plan.</param>
/// <param name="Unit">The unit it's sold in; products only.</param>
/// <param name="DescriptionText">A short description, cleaned as untrusted text.</param>
public sealed record CatalogItem(OfferingId Id, Sku Sku, string Name, OfferingKind Kind, UnitOfMeasure? Unit, string DescriptionText);

/// <summary>One page of search results.</summary>
/// <param name="Items">The offerings on this page.</param>
/// <param name="NextCursor">Pass this back to get the next page; <see langword="null"/> on the last page.</param>
public sealed record CatalogPage(IReadOnlyList<CatalogItem> Items, string? NextCursor);

/// <summary>Why a catalog search was refused.</summary>
public static class CatalogSearchErrors
{
    /// <summary>The query is empty.</summary>
    public static readonly DomainError QueryRequired = new(
        "SearchQueryRequired", "Give some words to search for, for example a product name, SKU or use.");

    /// <summary>The query is too long.</summary>
    public static readonly DomainError QueryTooLong = new(
        "SearchQueryTooLong", $"A search can be at most {SearchCatalog.MaxQueryLength} characters. Use a few key words.");

    /// <summary>The kind filter isn't one of the three kinds.</summary>
    public static readonly DomainError KindUnknown = new(
        "SearchKindUnknown", "The type must be product, service or plan, or left out to search everything.");

    /// <summary>The page size is out of range.</summary>
    public static readonly DomainError LimitOutOfRange = new(
        "SearchLimitOutOfRange", $"The limit must be between 1 and {SearchCatalog.MaxLimit}.");

    /// <summary>The cursor is malformed or from another search.</summary>
    public static readonly DomainError CursorInvalid = new(
        "SearchCursorInvalid", "The cursor isn't valid for this search. Repeat the search without a cursor to start again.");
}

/// <summary>
/// Searches the bound business's active offerings by SKU, name and description. Every word of the
/// query must match. An exact SKU comes first, then name matches, then description matches, each in
/// SKU order. Results never include prices, so the model has to ask the price resolver.
/// </summary>
/// <remarks>
/// M2 searches in memory over the active catalog, which is small (Acme has 118 active offerings).
/// A database-side or full-text search can replace it behind the same contract when catalogs grow.
/// </remarks>
/// <param name="catalog">Reads the catalog.</param>
public sealed class SearchCatalog(ICatalogReader catalog)
{
    /// <summary>Results per page when no limit is given.</summary>
    public const int DefaultLimit = 10;

    /// <summary>The most results per page.</summary>
    public const int MaxLimit = 50;

    /// <summary>The longest query accepted.</summary>
    public const int MaxQueryLength = 200;

    /// <summary>Descriptions in results are cut to this many characters.</summary>
    public const int DescriptionLength = 300;

    private const string CursorPrefix = "c1";

    /// <summary>Runs <paramref name="search"/> for <paramref name="businessId"/>.</summary>
    public async Task<Result<CatalogPage>> ExecuteAsync(BusinessId businessId, CatalogSearch search, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(search);

        var check = Check(search);
        if (check.IsFailure)
        {
            return Result.Failure<CatalogPage>(check.Error);
        }

        var (query, kind, limit) = check.Value;
        var fingerprint = Fingerprint(query, kind);
        var offset = 0;
        if (search.Cursor is { } cursor && !TryReadCursor(cursor, fingerprint, out offset))
        {
            return Result.Failure<CatalogPage>(CatalogSearchErrors.CursorInvalid);
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var offerings = await catalog.ListActiveOfferingsAsync(businessId, cancellationToken).ConfigureAwait(false);

        var matches = offerings
            .Where(o => kind is null || o.Kind == kind)
            .Select(o => (Offering: o, Rank: Rank(o, query, terms)))
            .Where(m => m.Rank is not null)
            .OrderBy(m => m.Rank)
            .ThenBy(m => m.Offering.Sku.Value, StringComparer.Ordinal)
            .Select(m => m.Offering)
            .ToList();

        var page = matches.Skip(offset).Take(limit).Select(ToItem).ToList();
        var next = offset + limit < matches.Count ? WriteCursor(offset + limit, fingerprint) : null;
        return Result.Success(new CatalogPage(page, next));
    }

    private static Result<(string Query, OfferingKind? Kind, int Limit)> Check(CatalogSearch search)
    {
        var raw = search.Query?.Trim() ?? string.Empty;
        if (raw.Length > MaxQueryLength)
        {
            return Result.Failure<(string, OfferingKind?, int)>(CatalogSearchErrors.QueryTooLong);
        }

        var query = UntrustedText.Clean(raw, MaxQueryLength);
        if (query.Length == 0)
        {
            return Result.Failure<(string, OfferingKind?, int)>(CatalogSearchErrors.QueryRequired);
        }

        OfferingKind? kind = null;
        if (!string.IsNullOrWhiteSpace(search.Kind))
        {
            // Match a name exactly: Enum.TryParse would also accept numbers and "Product, Plan".
            var name = Enum.GetNames<OfferingKind>().FirstOrDefault(n => n.Equals(search.Kind.Trim(), StringComparison.OrdinalIgnoreCase));
            if (name is null)
            {
                return Result.Failure<(string, OfferingKind?, int)>(CatalogSearchErrors.KindUnknown);
            }

            kind = Enum.Parse<OfferingKind>(name);
        }

        var limit = search.Limit ?? DefaultLimit;
        if (limit is < 1 or > MaxLimit)
        {
            return Result.Failure<(string, OfferingKind?, int)>(CatalogSearchErrors.LimitOutOfRange);
        }

        return Result.Success((query, kind, limit));
    }

    /// <summary>0 for an exact SKU, 1 when the name holds every word, 2 for the rest; null for no match.</summary>
    private static int? Rank(Offering offering, string query, string[] terms)
    {
        if (string.Equals(offering.Sku.Value, query, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var name = offering.Name.Normalize(NormalizationForm.FormKC);
        if (terms.All(t => name.Contains(t, StringComparison.OrdinalIgnoreCase) || offering.Sku.Value.Contains(t, StringComparison.OrdinalIgnoreCase)))
        {
            return 1;
        }

        var all = $"{offering.Sku.Value} {name} {offering.Description.Normalize(NormalizationForm.FormKC)}";
        return terms.All(t => all.Contains(t, StringComparison.OrdinalIgnoreCase)) ? 2 : null;
    }

    private static CatalogItem ToItem(Offering offering) => new(
        offering.Id,
        offering.Sku,
        UntrustedText.Clean(offering.Name, CatalogErrors.MaxNameLength),
        offering.Kind,
        offering is Product product ? product.Unit : null,
        UntrustedText.Clean(offering.Description, DescriptionLength));

    // The cursor carries the offset and a fingerprint of the search, so a cursor can't be reused
    // for a different query and land on an unrelated page.
    private static string Fingerprint(string query, OfferingKind? kind)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{query.ToUpperInvariant()}|{kind}"));
        return Convert.ToHexString(bytes, 0, 6);
    }

    private static string WriteCursor(int offset, string fingerprint) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Create(CultureInfo.InvariantCulture, $"{CursorPrefix}:{offset}:{fingerprint}")))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryReadCursor(string cursor, string fingerprint, out int offset)
    {
        offset = 0;
        if (cursor.Length is 0 or > 64)
        {
            return false;
        }

        var base64 = cursor.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + ((4 - (base64.Length % 4)) % 4), '=');

        Span<byte> bytes = stackalloc byte[64];
        if (!Convert.TryFromBase64String(base64, bytes, out var written))
        {
            return false;
        }

        var parts = Encoding.UTF8.GetString(bytes[..written]).Split(':');
        return parts.Length == 3
            && parts[0] == CursorPrefix
            && parts[2] == fingerprint
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out offset)
            && offset > 0;
    }
}
