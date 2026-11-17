using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Catalog;

/// <summary>How a service is priced. Pricing rules for each model arrive in M6.</summary>
public enum ServicePricingModel
{
    /// <summary>Hours worked times the rate, quoted as a range.</summary>
    TimeAndMaterials = 0,

    /// <summary>One price for defined deliverables.</summary>
    Fixed = 1,

    /// <summary>Time and materials with a cap.</summary>
    NotToExceed = 2,

    /// <summary>A monthly fee for a block of hours.</summary>
    Retainer = 3,
}

/// <summary>How often a plan bills.</summary>
public enum BillingPeriod
{
    /// <summary>Every month.</summary>
    Monthly = 0,

    /// <summary>Every year.</summary>
    Annual = 1,
}

/// <summary>
/// Work done by people. A stub until M6, which adds rate cards, scope estimates and availability.
/// Services can be catalogued now but can't be priced until then.
/// </summary>
public sealed class Service : Offering
{
    private Service(OfferingId id, BusinessId businessId, CommonFields common, VatCode vatCode, ServicePricingModel pricingModel)
        : base(id, businessId, common.Sku, common.Name, common.Description, vatCode) => PricingModel = pricingModel;

    /// <inheritdoc />
    public override OfferingKind Kind => OfferingKind.Service;

    /// <summary>How it is priced.</summary>
    public ServicePricingModel PricingModel { get; }

    /// <summary>Validates input and creates an active service.</summary>
    public static Result<Service> Create(
        OfferingId id,
        BusinessId businessId,
        string? sku,
        string? name,
        string? description,
        VatCode vatCode,
        ServicePricingModel pricingModel) =>
        Enum.IsDefined(pricingModel)
            ? ValidateCommon(sku, name, description, vatCode).Map(c => new Service(id, businessId, c, vatCode, pricingModel))
            : Result.Failure<Service>(CatalogErrors.OptionUnknown);
}

/// <summary>
/// A subscription or usage plan. A stub until M6, which adds pricing models, tiers, trials and terms.
/// Plans can be catalogued now but can't be priced until then.
/// </summary>
public sealed class Plan : Offering
{
    private Plan(OfferingId id, BusinessId businessId, CommonFields common, VatCode vatCode, BillingPeriod billingPeriod)
        : base(id, businessId, common.Sku, common.Name, common.Description, vatCode) => BillingPeriod = billingPeriod;

    /// <inheritdoc />
    public override OfferingKind Kind => OfferingKind.Plan;

    /// <summary>How often it bills.</summary>
    public BillingPeriod BillingPeriod { get; }

    /// <summary>Validates input and creates an active plan.</summary>
    public static Result<Plan> Create(
        OfferingId id,
        BusinessId businessId,
        string? sku,
        string? name,
        string? description,
        VatCode vatCode,
        BillingPeriod billingPeriod) =>
        Enum.IsDefined(billingPeriod)
            ? ValidateCommon(sku, name, description, vatCode).Map(c => new Plan(id, businessId, c, vatCode, billingPeriod))
            : Result.Failure<Plan>(CatalogErrors.OptionUnknown);
}
