using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Domain.Tests.Customers;

public class CustomerTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly PaymentTerms _thirtyDays = PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30).Value;

    private static Result<Customer> Create(
        string? accountNumber = "bld-0042",
        string? name = " Builders Warehouse Durban ",
        string? vatNumber = "4123 456 789",
        decimal creditLimit = 50_000m,
        decimal balance = 12_500m,
        string limitCurrency = "ZAR",
        string balanceCurrency = "ZAR",
        PriceListId? priceListId = null) =>
        Customer.Create(
            CustomerId.New(),
            _acme,
            accountNumber,
            name,
            vatNumber,
            new Money(creditLimit, limitCurrency),
            new Money(balance, balanceCurrency),
            _thirtyDays,
            priceListId,
            allowBackOrders: true,
            allowPartialShipment: false);

    [Fact]
    public void Creates_a_customer_with_its_fields()
    {
        var priceList = PriceListId.New();
        var customer = Create(priceListId: priceList).Value;

        Assert.Equal(_acme, customer.BusinessId);
        Assert.Equal("BLD-0042", customer.AccountNumber.Value);
        Assert.Equal("Builders Warehouse Durban", customer.Name);
        Assert.Equal("4123456789", customer.VatNumber?.Value);
        Assert.Equal(new Money(50_000m, "ZAR"), customer.CreditLimit);
        Assert.Equal(new Money(12_500m, "ZAR"), customer.Balance);
        Assert.Equal(_thirtyDays, customer.PaymentTerms);
        Assert.Equal(priceList, customer.PriceListId);
        Assert.True(customer.AllowBackOrders);
        Assert.False(customer.AllowPartialShipment);
        Assert.False(customer.OnHold);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void VAT_number_is_optional(string? vatNumber) =>
        Assert.Null(Create(vatNumber: vatNumber).Value.VatNumber);

    [Fact]
    public void A_customer_in_credit_has_a_negative_balance() =>
        Assert.True(Create(balance: -800m).Value.Balance.IsNegative);

    [Fact]
    public void A_zero_credit_limit_means_no_credit() =>
        Assert.True(Create(creditLimit: 0m).Value.CreditLimit.IsZero);

    public static TheoryData<string?, string?, string?, decimal, string, string> InvalidInputs => new()
    {
        { "B", "Name", null, 0m, "ZAR", "AccountNumberInvalid" },
        { "BLD_42", "Name", null, 0m, "ZAR", "AccountNumberInvalid" },
        { "BLD-42", " ", null, 0m, "ZAR", "NameRequired" },
        { "BLD-42", new string('n', 201), null, 0m, "ZAR", "NameTooLong" },
        { "BLD-42", "Name", "5123456789", 0m, "ZAR", "VatNumberInvalid" },
        { "BLD-42", "Name", null, -1m, "ZAR", "CreditLimitNegative" },
        { "BLD-42", "Name", null, 0m, "USD", "CurrencyMismatch" },
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Rejects_invalid_input_with_a_code(
        string? accountNumber, string? name, string? vatNumber, decimal creditLimit, string balanceCurrency, string expectedCode) =>
        Assert.Equal(
            expectedCode,
            Create(accountNumber, name, vatNumber, creditLimit, balanceCurrency: balanceCurrency).Error.Code);

    [Fact]
    public void Rejects_money_with_no_currency()
    {
        var result = Customer.Create(
            CustomerId.New(), _acme, "BLD-42", "Name", null, default, default, _thirtyDays, null, false, false);

        Assert.Equal(CustomerErrors.CurrencyInvalid, result.Error);
    }

    [Fact]
    public void Can_be_held_and_released()
    {
        var customer = Create().Value;

        customer.PlaceOnHold();
        Assert.True(customer.OnHold);

        customer.ReleaseHold();
        Assert.False(customer.OnHold);
    }
}

public class CustomerValueTests
{
    [Theory]
    [InlineData("A1")]
    [InlineData("12345678901234567890")]
    public void Accepts_account_numbers_at_the_length_limits(string input) =>
        Assert.True(AccountNumber.Create(input).IsSuccess);

    [Theory]
    [InlineData(null)]
    [InlineData("-BLD")]
    [InlineData("123456789012345678901")]
    public void Rejects_bad_account_numbers(string? input) =>
        Assert.Equal(CustomerErrors.AccountNumberInvalid, AccountNumber.Create(input).Error);

    [Theory]
    [InlineData("4123456789")]
    [InlineData(" 4123 456 789 ")]
    public void Accepts_south_african_vat_numbers(string input) =>
        Assert.Equal("4123456789", VatNumber.Create(input).Value.Value);

    [Theory]
    [InlineData(null)]
    [InlineData("412345678")]
    [InlineData("41234567890")]
    [InlineData("3123456789")]
    [InlineData("41234S6789")]
    public void Rejects_bad_vat_numbers(string? input) =>
        Assert.Equal(CustomerErrors.VatNumberInvalid, VatNumber.Create(input).Error);

    [Fact]
    public void Value_types_print_their_value()
    {
        Assert.Equal("BLD-1", AccountNumber.Create("bld-1").Value.ToString());
        Assert.Equal("4123456789", VatNumber.Create("4123456789").Value.ToString());
    }
}

public class PaymentTermsTests
{
    [Theory]
    [InlineData(PaymentTermsBasis.CashWithOrder, 0)]
    [InlineData(PaymentTermsBasis.DaysFromInvoice, 1)]
    [InlineData(PaymentTermsBasis.DaysFromEndOfMonth, 120)]
    public void Accepts_valid_terms(PaymentTermsBasis basis, int days) =>
        Assert.True(PaymentTerms.Create(basis, days).IsSuccess);

    [Theory]
    [InlineData(PaymentTermsBasis.CashWithOrder, 30)]
    [InlineData(PaymentTermsBasis.DaysFromInvoice, 0)]
    [InlineData(PaymentTermsBasis.DaysFromInvoice, -5)]
    [InlineData(PaymentTermsBasis.DaysFromEndOfMonth, 121)]
    public void Rejects_days_that_do_not_suit_the_basis(PaymentTermsBasis basis, int days) =>
        Assert.Equal(CustomerErrors.PaymentDaysInvalid, PaymentTerms.Create(basis, days).Error);

    [Fact]
    public void Rejects_an_unknown_basis() =>
        Assert.Equal("OptionUnknown", PaymentTerms.Create((PaymentTermsBasis)9, 30).Error.Code);

    [Fact]
    public void Default_terms_are_cash_with_order() =>
        Assert.Equal(PaymentTerms.CashWithOrder, default);

    [Theory]
    [InlineData(PaymentTermsBasis.CashWithOrder, 0, "2026-11-18", "2026-11-18")]
    [InlineData(PaymentTermsBasis.DaysFromInvoice, 30, "2026-11-18", "2026-12-18")]
    [InlineData(PaymentTermsBasis.DaysFromEndOfMonth, 30, "2026-11-18", "2026-12-30")]
    [InlineData(PaymentTermsBasis.DaysFromEndOfMonth, 30, "2028-02-03", "2028-03-30")]
    public void Works_out_the_due_date(PaymentTermsBasis basis, int days, string invoiced, string due)
    {
        var terms = basis == PaymentTermsBasis.CashWithOrder ? PaymentTerms.CashWithOrder : PaymentTerms.Create(basis, days).Value;

        Assert.Equal(DateOnly.Parse(due, System.Globalization.CultureInfo.InvariantCulture), terms.DueDate(DateOnly.Parse(invoiced, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Describes_itself()
    {
        Assert.Equal("Cash with order", PaymentTerms.CashWithOrder.ToString());
        Assert.Equal("30 days from invoice", PaymentTerms.Create(PaymentTermsBasis.DaysFromInvoice, 30).Value.ToString());
        Assert.Equal("60 days from end of month", PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 60).Value.ToString());
    }
}

public class CustomerSegmentTests
{
    [Theory]
    [InlineData(0, CustomerSegment.New)]
    [InlineData(1, CustomerSegment.Existing)]
    [InlineData(42, CustomerSegment.Existing)]
    public void Derives_the_segment_from_completed_orders(int completedOrders, CustomerSegment expected) =>
        Assert.Equal(expected, CustomerSegments.From(completedOrders));

    [Fact]
    public void Rejects_a_negative_count() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => CustomerSegments.From(-1));
}
