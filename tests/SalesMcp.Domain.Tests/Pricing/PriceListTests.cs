using SalesMcp.Domain.Common;
using SalesMcp.Domain.Pricing;

namespace SalesMcp.Domain.Tests.Pricing;

public class PriceListTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly OfferingId _bearing = OfferingId.New();

    private static PriceList NewList() => PriceList.Create(PriceListId.New(), _acme, " Trade 2026 ", "ZAR").Value;

    [Fact]
    public void Creates_an_empty_list()
    {
        var list = NewList();

        Assert.Equal("Trade 2026", list.Name);
        Assert.Equal("ZAR", list.Currency);
        Assert.Equal(_acme, list.BusinessId);
        Assert.Empty(list.Items);
    }

    [Theory]
    [InlineData(null, "ZAR", "NameRequired")]
    [InlineData("List", "zar", "CurrencyInvalid")]
    [InlineData("List", null, "CurrencyInvalid")]
    public void Rejects_invalid_lists(string? name, string? currency, string expectedCode) =>
        Assert.Equal(expectedCode, PriceList.Create(PriceListId.New(), _acme, name, currency).Error.Code);

    [Fact]
    public void Rejects_a_long_name() =>
        Assert.Equal(PricingErrors.NameTooLong, PriceList.Create(PriceListId.New(), _acme, new string('n', 201), "ZAR").Error);

    [Fact]
    public void Adds_quantity_breaks()
    {
        var list = NewList();

        Assert.True(list.AddItem(_bearing, new Quantity(1), new Money(100m, "ZAR")).IsSuccess);
        Assert.True(list.AddItem(_bearing, new Quantity(10), new Money(90m, "ZAR")).IsSuccess);
        Assert.Equal(2, list.Items.Count);
    }

    [Theory]
    [InlineData(0, "ZAR", "PriceNotPositive")]
    [InlineData(-5, "ZAR", "PriceNotPositive")]
    [InlineData(100, "USD", "CurrencyMismatch")]
    public void Rejects_bad_prices(decimal amount, string currency, string expectedCode) =>
        Assert.Equal(expectedCode, NewList().AddItem(_bearing, new Quantity(1), new Money(amount, currency)).Error.Code);

    [Fact]
    public void Rejects_a_price_with_no_currency() =>
        Assert.Equal(PricingErrors.CurrencyMismatch, NewList().AddItem(_bearing, new Quantity(1), default).Error);

    [Fact]
    public void Rejects_a_second_price_at_the_same_break()
    {
        var list = NewList();
        list.AddItem(_bearing, new Quantity(10), new Money(90m, "ZAR"));

        Assert.Equal(PricingErrors.BreakDuplicate, list.AddItem(_bearing, new Quantity(10), new Money(85m, "ZAR")).Error);
        Assert.True(list.AddItem(OfferingId.New(), new Quantity(10), new Money(85m, "ZAR")).IsSuccess);
    }

    [Fact]
    public void Best_break_is_null_when_the_offering_is_not_listed() =>
        Assert.Null(NewList().BestBreak(_bearing, new Quantity(1)));
}

public class ContractPriceTests
{
    private static readonly DateOnly _from = new(2026, 1, 1);

    [Fact]
    public void Creates_a_contract_price()
    {
        var customer = CustomerId.New();
        var offering = OfferingId.New();
        var contract = ContractPrice.Create(customer, offering, new Money(70m, "ZAR"), _from, _from).Value;

        Assert.Equal(customer, contract.CustomerId);
        Assert.Equal(offering, contract.OfferingId);
        Assert.Equal(new Money(70m, "ZAR"), contract.UnitPrice);
        Assert.True(contract.IsValidOn(_from));
        Assert.False(contract.IsValidOn(_from.AddDays(1)));
        Assert.False(contract.IsValidOn(_from.AddDays(-1)));
    }

    [Fact]
    public void Rejects_an_end_before_the_start() =>
        Assert.Equal(
            PricingErrors.ContractDatesInvalid,
            ContractPrice.Create(CustomerId.New(), OfferingId.New(), new Money(70m, "ZAR"), _from, _from.AddDays(-1)).Error);

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Rejects_a_price_that_is_not_positive(decimal amount) =>
        Assert.Equal(
            PricingErrors.PriceNotPositive,
            ContractPrice.Create(CustomerId.New(), OfferingId.New(), new Money(amount, "ZAR"), _from, _from).Error);

    [Fact]
    public void Rejects_a_price_with_no_currency() =>
        Assert.Equal(
            PricingErrors.CurrencyInvalid,
            ContractPrice.Create(CustomerId.New(), OfferingId.New(), default, _from, _from).Error);
}
