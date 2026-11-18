using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Domain.Tests.Customers;

public class ContactTests
{
    private static readonly CustomerId _customer = CustomerId.New();
    private static readonly DateTimeOffset _when = new(2026, 11, 18, 9, 30, 0, TimeSpan.FromHours(2));

    private static Result<Contact> Create(string? name = " Thandi Mokoena ", string? email = " Thandi.Mokoena@Example.co.za ") =>
        Contact.Create(ContactId.New(), _customer, name, email);

    [Fact]
    public void Creates_a_contact_with_no_consent_record()
    {
        var contact = Create().Value;

        Assert.Equal(_customer, contact.CustomerId);
        Assert.Equal("Thandi Mokoena", contact.Name);
        Assert.Equal("thandi.mokoena@example.co.za", contact.Email);
        Assert.Null(contact.MarketingConsent);
        Assert.False(contact.MayReceiveMarketing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Email_is_optional(string? email) => Assert.Null(Create(email: email).Value.Email);

    [Theory]
    [InlineData("thandi")]
    [InlineData("@example.co.za")]
    [InlineData("thandi@")]
    [InlineData("thandi@example")]
    [InlineData("thandi@.example")]
    [InlineData("thandi@example.")]
    [InlineData("thandi@@example.co.za")]
    [InlineData("thandi@exa@mple.co.za")]
    [InlineData("than di@example.co.za")]
    public void Rejects_implausible_emails(string email) =>
        Assert.Equal(CustomerErrors.EmailInvalid, Create(email: email).Error);

    [Fact]
    public void Rejects_an_email_over_the_length_limit()
    {
        var local = new string('a', 64);
        var domain = new string('d', 254 - 64 - 1 - 3) + ".za";

        Assert.True(Create(email: $"{local}@{domain}").IsSuccess);
        Assert.Equal(CustomerErrors.EmailInvalid, Create(email: $"{local}a@{domain}").Error);
    }

    [Theory]
    [InlineData(null, "NameRequired")]
    [InlineData(" ", "NameRequired")]
    public void Requires_a_name(string? name, string expectedCode) =>
        Assert.Equal(expectedCode, Create(name: name).Error.Code);

    [Fact]
    public void Rejects_a_long_name() =>
        Assert.Equal(CustomerErrors.NameTooLong, Create(name: new string('n', 201)).Error);

    [Fact]
    public void An_opt_in_allows_marketing()
    {
        var contact = Create().Value;
        var consent = MarketingConsent.OptIn(_when, ConsentSource.WebForm).Value;

        contact.RecordMarketingConsent(consent);

        Assert.True(contact.MayReceiveMarketing);
        Assert.Equal(consent, contact.MarketingConsent);
        Assert.Equal(_when, consent.RecordedAt);
        Assert.Equal(ConsentSource.WebForm, consent.Source);
    }

    [Fact]
    public void A_later_opt_out_replaces_the_opt_in()
    {
        var contact = Create().Value;
        contact.RecordMarketingConsent(MarketingConsent.OptIn(_when, ConsentSource.WebForm).Value);

        contact.RecordMarketingConsent(MarketingConsent.OptOut(_when.AddDays(10), ConsentSource.Email).Value);

        Assert.False(contact.MayReceiveMarketing);
        Assert.False(contact.MarketingConsent!.OptedIn);
    }

    [Fact]
    public void Rejects_a_null_consent() =>
        Assert.Throws<ArgumentNullException>(() => Create().Value.RecordMarketingConsent(null!));
}

public class MarketingConsentTests
{
    [Fact]
    public void Requires_a_time() =>
        Assert.Equal(CustomerErrors.ConsentTimeRequired, MarketingConsent.OptIn(default, ConsentSource.Phone).Error);

    [Fact]
    public void Rejects_an_unknown_source() =>
        Assert.Equal(
            "OptionUnknown",
            MarketingConsent.OptOut(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), (ConsentSource)9).Error.Code);
}
