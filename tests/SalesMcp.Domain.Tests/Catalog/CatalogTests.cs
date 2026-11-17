using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Tests.Catalog;

public class SkuTests
{
    [Theory]
    [InlineData("ACM-BRG-6204", "ACM-BRG-6204")]
    [InlineData("  acm-brg-6204 ", "ACM-BRG-6204")]
    [InlineData("A1", "A1")]
    [InlineData("12345678901234567890123456789012", "12345678901234567890123456789012")]
    public void Normalises_valid_skus_to_upper_case(string input, string expected) =>
        Assert.Equal(expected, Sku.Create(input).Value.Value);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("-ACM")]
    [InlineData("ACM_BRG")]
    [InlineData("ACM BRG")]
    [InlineData("ÄCM-1")]
    [InlineData("123456789012345678901234567890123")]
    public void Rejects_invalid_skus(string? input) =>
        Assert.Equal(CatalogErrors.SkuInvalid, Sku.Create(input).Error);

    [Fact]
    public void Skus_differing_only_in_case_are_equal() =>
        Assert.Equal(Sku.Create("abc-1").Value, Sku.Create("ABC-1").Value);
}

public class BusinessTests
{
    [Fact]
    public void Creates_a_business()
    {
        var business = Business.Create(BusinessId.New(), " Acme Distributors ", "ZAR", "en-ZA").Value;

        Assert.Equal("Acme Distributors", business.Name);
        Assert.Equal("ZAR", business.Currency);
        Assert.Equal("en-ZA", business.Locale);
    }

    [Theory]
    [InlineData("", "ZAR", "en-ZA", "NameRequired")]
    [InlineData("Acme", "zar", "en-ZA", "CurrencyInvalid")]
    [InlineData("Acme", "RAND", "en-ZA", "CurrencyInvalid")]
    [InlineData("Acme", "ZAR", "en", "LocaleInvalid")]
    [InlineData("Acme", "ZAR", "EN-za", "LocaleInvalid")]
    [InlineData("Acme", "ZAR", null, "LocaleInvalid")]
    public void Rejects_invalid_input(string? name, string? currency, string? locale, string expectedCode) =>
        Assert.Equal(expectedCode, Business.Create(BusinessId.New(), name, currency, locale).Error.Code);
}

public class ServiceAndPlanStubTests
{
    private static readonly BusinessId _nimbus = BusinessId.New();

    [Fact]
    public void Service_is_an_offering_of_kind_service()
    {
        var service = Service.Create(
            OfferingId.New(), _nimbus, "NIM-NET-AUDIT", "Network audit", "", VatCode.Standard, ServicePricingModel.Fixed).Value;

        Assert.Equal(OfferingKind.Service, service.Kind);
        Assert.Equal(ServicePricingModel.Fixed, service.PricingModel);
        Assert.True(service.IsActive);
    }

    [Fact]
    public void Plan_is_an_offering_of_kind_plan()
    {
        var plan = Plan.Create(
            OfferingId.New(), _nimbus, "LL-STARTER-M", "LedgerLite Starter", "", VatCode.Standard, BillingPeriod.Monthly).Value;

        Assert.Equal(OfferingKind.Plan, plan.Kind);
        Assert.Equal(BillingPeriod.Monthly, plan.BillingPeriod);
    }

    [Fact]
    public void Stubs_share_the_common_validation()
    {
        Assert.Equal("SkuInvalid", Service.Create(OfferingId.New(), _nimbus, "x", "Audit", "", VatCode.Standard, ServicePricingModel.Fixed).Error.Code);
        Assert.Equal("NameRequired", Plan.Create(OfferingId.New(), _nimbus, "LL-1", " ", "", VatCode.Standard, BillingPeriod.Annual).Error.Code);
    }

    [Fact]
    public void Stubs_reject_unknown_options()
    {
        Assert.Equal("OptionUnknown", Service.Create(OfferingId.New(), _nimbus, "NIM-1", "Audit", "", VatCode.Standard, (ServicePricingModel)9).Error.Code);
        Assert.Equal("OptionUnknown", Plan.Create(OfferingId.New(), _nimbus, "LL-1", "Plan", "", VatCode.Standard, (BillingPeriod)9).Error.Code);
    }

    [Fact]
    public void All_three_kinds_can_be_handled_as_offerings()
    {
        Offering[] catalog =
        [
            Product.Create(OfferingId.New(), _nimbus, "P-1", "Router", "", VatCode.Standard, UnitOfMeasure.Each, 3, 14).Value,
            Service.Create(OfferingId.New(), _nimbus, "S-1", "Install", "", VatCode.Standard, ServicePricingModel.TimeAndMaterials).Value,
            Plan.Create(OfferingId.New(), _nimbus, "L-1", "Support", "", VatCode.Standard, BillingPeriod.Annual).Value,
        ];

        Assert.Equal([OfferingKind.Product, OfferingKind.Service, OfferingKind.Plan], catalog.Select(o => o.Kind));
    }
}
