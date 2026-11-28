using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Catalog;

/// <summary>The three ways to sell (ADR-0003).</summary>
public enum OfferingKind
{
    /// <summary>Physical goods: stock, shipping, returns.</summary>
    Product = 0,

    /// <summary>Work done by people: scope, rates, availability (M6).</summary>
    Service = 1,

    /// <summary>A subscription or usage plan: periods, tiers, trials (M6).</summary>
    Plan = 2,
}

/// <summary>
/// Anything a business sells. One hierarchy so the catalog is searched in one place;
/// each kind adds what it needs to be sold.
/// </summary>
/// <remarks>
/// Prices are not part of an offering. They live in price lists and contracts and come only from
/// the price resolver (guardrail A1).
/// </remarks>
public abstract class Offering
{
#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    protected Offering()
    {
    }
#pragma warning restore CS8618

    /// <summary>Sets the fields every offering has. Inputs must already be validated.</summary>
    protected Offering(OfferingId id, BusinessId businessId, Sku sku, string name, string description, VatCode vatCode)
    {
        Id = id;
        BusinessId = businessId;
        Sku = sku;
        Name = name;
        Description = description;
        VatCode = vatCode;
        Category = string.Empty;
        IsActive = true;
    }

    /// <summary>The offering's id.</summary>
    public OfferingId Id { get; }

    /// <summary>The business that sells it.</summary>
    public BusinessId BusinessId { get; }

    /// <summary>Human-facing code, unique within the business.</summary>
    public Sku Sku { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>
    /// Free text describing the offering. Untrusted: it may contain anything a catalog editor typed,
    /// including text that looks like instructions. It is stored as entered and must be fenced and
    /// sanitised whenever it is returned to a model (guardrail D4).
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// The catalog group it is listed under, such as "Fasteners"; empty when it has none. Free text
    /// from the catalog, so it is cleaned like the name when shown.
    /// </summary>
    public string Category { get; private set; }

    /// <summary>How VAT applies.</summary>
    public VatCode VatCode { get; }

    /// <summary>Inactive offerings can't be priced, quoted or recommended.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Which of the three kinds this is.</summary>
    public abstract OfferingKind Kind { get; }

    /// <summary>Lists the offering under <paramref name="category"/>; blank removes it from any group.</summary>
    public Result Categorise(string? category)
    {
        var trimmed = category?.Trim() ?? string.Empty;
        if (trimmed.Length > CatalogErrors.MaxCategoryLength)
        {
            return Result.Failure(CatalogErrors.CategoryTooLong);
        }

        Category = trimmed;
        return Result.Success();
    }

    /// <summary>Stops the offering being sold. Existing quotes keep their snapshot.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Makes the offering available for sale again.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Checks the fields shared by all kinds; returns the cleaned values or the first failure.</summary>
    protected static Result<CommonFields> ValidateCommon(string? sku, string? name, string? description, VatCode vatCode)
    {
        var parsedSku = Sku.Create(sku);
        if (parsedSku.IsFailure)
        {
            return Result.Failure<CommonFields>(parsedSku.Error);
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<CommonFields>(CatalogErrors.NameRequired);
        }

        if (trimmedName.Length > CatalogErrors.MaxNameLength)
        {
            return Result.Failure<CommonFields>(CatalogErrors.NameTooLong);
        }

        var text = description ?? string.Empty;
        if (text.Length > CatalogErrors.MaxDescriptionLength)
        {
            return Result.Failure<CommonFields>(CatalogErrors.DescriptionTooLong);
        }

        if (!Enum.IsDefined(vatCode))
        {
            return Result.Failure<CommonFields>(CatalogErrors.OptionUnknown);
        }

        return Result.Success(new CommonFields(parsedSku.Value, trimmedName, text));
    }

    /// <summary>Validated values shared by every kind of offering.</summary>
    /// <param name="Sku">The parsed SKU.</param>
    /// <param name="Name">The trimmed name.</param>
    /// <param name="Description">The description, as entered.</param>
    protected sealed record CommonFields(Sku Sku, string Name, string Description);
}
