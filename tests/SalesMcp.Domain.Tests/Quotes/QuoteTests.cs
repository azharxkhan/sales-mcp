using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Orders;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Domain.Tests.Quotes;

/// <summary>A small Acme catalog priced through the real resolver.</summary>
public abstract class QuoteFixture
{
    protected static readonly DateOnly Today = new(2026, 11, 18);
    protected static readonly DateTimeOffset Now = new(2026, 11, 18, 10, 0, 0, TimeSpan.FromHours(2));

    protected QuoteFixture()
    {
        Bearing = NewProduct("ACM-BRG-6204", VatCode.Standard);
        Gloves = NewProduct("ACM-GLV-NIT", VatCode.Standard);
        Bread = NewProduct("ACM-CAN-BRD", VatCode.ZeroRated);

        ListPrices = PriceList.Create(PriceListId.New(), Acme, "List 2026", "ZAR").Value;
        ListPrices.AddItem(Bearing.Id, new Quantity(1), Zar(100m));
        ListPrices.AddItem(Bearing.Id, new Quantity(10), Zar(90m));
        ListPrices.AddItem(Gloves.Id, new Quantity(1), Zar(45.50m));
        ListPrices.AddItem(Bread.Id, new Quantity(1), Zar(18.99m));

        Customer = NewCustomer("BLD-0042");
        Agent = AgentId.Create("acme-distributor-jolt").Value;
        Number = QuoteNumber.From(2026, 123).Value;
    }

    protected BusinessId Acme { get; } = BusinessId.New();

    protected PriceResolver Resolver { get; } = new(VatRateTable.SouthAfrica);

    protected Product Bearing { get; }

    protected Product Gloves { get; }

    protected Product Bread { get; }

    protected PriceList ListPrices { get; }

    protected Customer Customer { get; }

    protected AgentId Agent { get; }

    protected QuoteNumber Number { get; }

    protected static Money Zar(decimal amount) => new(amount, "ZAR");

    protected Product NewProduct(string sku, VatCode vatCode, BusinessId? business = null) =>
        Product.Create(OfferingId.New(), business ?? Acme, sku, sku + " name", "", vatCode, UnitOfMeasure.Each, 7, 30).Value;

    protected Customer NewCustomer(string account) =>
        SalesMcp.Domain.Customers.Customer.Create(
            CustomerId.New(), Acme, account, account, null, Zar(50_000m), Zar(0m),
            PaymentTerms.CashWithOrder, null, true, true).Value;

    protected PricedOffering Priced(Offering offering, decimal quantity, decimal discount = 0m, Customer? customer = null) =>
        new(offering, Resolver.Resolve(
            new PriceRequest(offering, customer ?? Customer, new Quantity(quantity), new Percentage(discount), Now),
            new PriceSources(ListPrices, null, [])).Value);

    protected Result<Quote> NewQuote(
        IReadOnlyList<PricedOffering> lines,
        IEnumerable<RiskReducer>? riskReducers = null,
        int validDays = QuoteErrors.DefaultValidDays) =>
        Quote.Create(QuoteId.New(), Number, Customer, Agent, Today, lines, riskReducers ?? [], validDays);
}

public class QuoteTests : QuoteFixture
{
    [Fact]
    public void Can_be_declined_once_while_open()
    {
        var quote = NewQuote([Priced(Bearing, 1)]).Value;

        Assert.True(quote.Decline().IsSuccess);
        Assert.Equal(QuoteStatus.Declined, quote.Status);
        Assert.Equal(QuoteErrors.NotOpen, quote.Decline().Error);
        Assert.Equal(QuoteErrors.NotOpen, quote.CanBeAccepted(Today).Error);
    }

    [Fact]
    public void Cannot_be_declined_once_accepted()
    {
        var quote = NewQuote([Priced(Bearing, 1)]).Value;
        SalesOrder.FromQuote(OrderId.New(), quote, Today);

        Assert.Equal(QuoteErrors.NotOpen, quote.Decline().Error);
        Assert.Equal(QuoteStatus.Accepted, quote.Status);
    }

    [Fact]
    public void Issues_a_quote_valid_for_30_days_by_default()
    {
        var quote = NewQuote([Priced(Bearing, 2)]).Value;

        Assert.Equal(Number, quote.Number);
        Assert.Equal(Customer.Id, quote.CustomerId);
        Assert.Equal(Agent, quote.AgentId);
        Assert.Equal(QuoteStatus.Issued, quote.Status);
        Assert.Equal(Today, quote.IssuedOn);
        Assert.Equal(new DateOnly(2026, 12, 18), quote.ValidUntil);
        Assert.Equal("ZAR", quote.Currency);
    }

    [Fact]
    public void Numbers_lines_from_one_and_snapshots_the_item()
    {
        var quote = NewQuote([Priced(Bearing, 2), Priced(Gloves, 5)]).Value;

        Assert.Equal([1, 2], quote.Lines.Select(l => l.LineNumber));
        Assert.Equal(Bearing.Id, quote.Lines[0].OfferingId);
        Assert.Equal("ACM-BRG-6204", quote.Lines[0].Sku.Value);
        Assert.Equal("ACM-BRG-6204 name", quote.Lines[0].Name);
        Assert.Equal(new Quantity(2), quote.Lines[0].Quantity);
    }

    [Fact]
    public void Totals_equal_the_sum_of_the_lines()
    {
        var quote = NewQuote([Priced(Bearing, 12, discount: 5m), Priced(Gloves, 3), Priced(Bread, 7)]).Value;

        Assert.Equal(Money.Sum(quote.Lines.Select(l => l.Net), "ZAR"), quote.Totals.Net);
        Assert.Equal(Money.Sum(quote.Lines.Select(l => l.Vat), "ZAR"), quote.Totals.Vat);
        Assert.Equal(Money.Sum(quote.Lines.Select(l => l.Total), "ZAR"), quote.Totals.Total);

        // 12 x 90 = 1080 less 5% (54) = 1026; 3 x 45.50 = 136.50; 7 x 18.99 = 132.93 zero-rated.
        Assert.Equal(Zar(1_295.43m), quote.Totals.Net);
        Assert.Equal(Zar(174.38m), quote.Totals.Vat); // 153.90 + 20.48 (20.475) + 0
        Assert.Equal(Zar(1_469.81m), quote.Totals.Total);
        Assert.Equal(Zar(54m), quote.Discount);
        Assert.Equal([VatCode.Standard, VatCode.ZeroRated], quote.Totals.ByCode.Select(c => c.Code));
    }

    [Fact]
    public void The_snapshot_is_unaffected_by_later_price_and_catalog_changes()
    {
        var quote = NewQuote([Priced(Bearing, 5)]).Value;

        ListPrices.AddItem(Bearing.Id, new Quantity(5), Zar(60m));
        Bearing.Deactivate();

        Assert.Equal(Zar(100m), quote.Lines[0].Price.UnitPrice);
        Assert.Equal(Zar(575m), quote.Totals.Total);

        Bearing.Activate();
        Assert.Equal(Zar(60m), Priced(Bearing, 5).Price.UnitPrice);
    }

    [Fact]
    public void The_snapshot_steps_are_a_copy()
    {
        var priced = Priced(Bearing, 1);
        var original = (List<PriceStep>)priced.Price.Steps;
        var quote = NewQuote([priced]).Value;

        original.Clear();

        Assert.NotEmpty(quote.Lines[0].Price.Steps);
    }

    [Theory]
    [InlineData("2026-11-18", false)]
    [InlineData("2026-12-18", false)]
    [InlineData("2026-12-19", true)]
    public void Expires_after_the_valid_until_date(string today, bool expired) =>
        Assert.Equal(
            expired,
            NewQuote([Priced(Bearing, 1)]).Value.IsExpired(DateOnly.Parse(today, System.Globalization.CultureInfo.InvariantCulture)));

    [Theory]
    [InlineData(1, "2026-11-19")]
    [InlineData(90, "2027-02-16")]
    public void Validity_can_be_set(int days, string validUntil) =>
        Assert.Equal(
            DateOnly.Parse(validUntil, System.Globalization.CultureInfo.InvariantCulture),
            NewQuote([Priced(Bearing, 1)], validDays: days).Value.ValidUntil);

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public void Rejects_validity_out_of_range(int days) =>
        Assert.Equal(QuoteErrors.ValidityInvalid, NewQuote([Priced(Bearing, 1)], validDays: days).Error);

    [Fact]
    public void Keeps_risk_reducers_without_repeats() =>
        Assert.Equal(
            [RiskReducer.ReturnWindow, RiskReducer.PhasedDelivery],
            NewQuote([Priced(Bearing, 1)], [RiskReducer.ReturnWindow, RiskReducer.PhasedDelivery, RiskReducer.ReturnWindow])
                .Value.RiskReducers);

    [Fact]
    public void Rejects_an_unknown_risk_reducer() =>
        Assert.Equal(QuoteErrors.OptionUnknown, NewQuote([Priced(Bearing, 1)], [(RiskReducer)9]).Error);

    [Fact]
    public void Rejects_an_empty_quote() => Assert.Equal(QuoteErrors.Empty, NewQuote([]).Error);

    [Fact]
    public void Rejects_a_line_priced_for_another_customer()
    {
        var other = NewCustomer("BLD-0099");

        Assert.Equal(QuoteErrors.CustomerMismatch, NewQuote([Priced(Bearing, 1, customer: other)]).Error);
    }

    [Fact]
    public void Rejects_a_price_for_a_different_item() =>
        Assert.Equal(
            QuoteErrors.LineOfferingMismatch,
            NewQuote([new PricedOffering(Gloves, Priced(Bearing, 1).Price)]).Error);

    [Fact]
    public void Rejects_an_item_from_another_business()
    {
        var foreign = NewProduct("ZZZ-1", VatCode.Standard, BusinessId.New());

        Assert.Equal(
            QuoteErrors.BusinessMismatch,
            NewQuote([new PricedOffering(foreign, Priced(Bearing, 1).Price)]).Error);
    }

    [Fact]
    public void Rejects_the_same_item_on_two_lines() =>
        Assert.Equal(QuoteErrors.LineDuplicate, NewQuote([Priced(Bearing, 5), Priced(Bearing, 5)]).Error);

    [Fact]
    public void Rejects_lines_in_different_currencies()
    {
        var dollars = Priced(Gloves, 1);
        var price = dollars.Price with { UnitPrice = new Money(3m, "USD") };

        Assert.Equal(
            QuoteErrors.CurrencyMismatch,
            NewQuote([Priced(Bearing, 1), dollars with { Price = price }]).Error);
    }

    [Fact]
    public void Rejects_null_arguments()
    {
        Assert.Throws<ArgumentNullException>(() =>
            Quote.Create(QuoteId.New(), Number, null!, Agent, Today, [], []));
        Assert.Throws<ArgumentNullException>(() =>
            Quote.Create(QuoteId.New(), Number, Customer, Agent, Today, null!, []));
        Assert.Throws<ArgumentNullException>(() =>
            Quote.Create(QuoteId.New(), Number, Customer, Agent, Today, [], null!));
    }
}

public class SalesOrderTests : QuoteFixture
{
    [Fact]
    public void Converts_a_quote_into_a_draft_order_at_the_quoted_prices()
    {
        var quote = NewQuote([Priced(Bearing, 12, discount: 5m), Priced(Bread, 7)]).Value;

        var order = SalesOrder.FromQuote(OrderId.New(), quote, Today.AddDays(3)).Value;

        Assert.Equal(quote.Id, order.SourceQuoteId);
        Assert.Equal(Customer.Id, order.CustomerId);
        Assert.Equal(Agent, order.AgentId);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Equal(Today.AddDays(3), order.OrderedOn);
        Assert.Equal("ZAR", order.Currency);
        Assert.Equal([1, 2], order.Lines.Select(l => l.LineNumber));
        Assert.Equal(Zar(90m), order.Lines[0].UnitPrice);
        Assert.Equal(quote.Lines[0].Sku, order.Lines[0].Sku);
        Assert.Equal(quote.Lines[0].Name, order.Lines[0].Name);
        Assert.Equal(quote.Lines[0].OfferingId, order.Lines[0].OfferingId);
        Assert.Equal(new Quantity(12), order.Lines[0].Quantity);
        Assert.Equal(quote.Lines[1].Net, order.Lines[1].Net);
        Assert.Equal(quote.Totals.Total, order.Totals.Total);
        Assert.Equal(Money.Sum(order.Lines.Select(l => l.Total), "ZAR"), order.Totals.Total);
    }

    [Fact]
    public void Marks_the_quote_accepted_so_it_converts_only_once()
    {
        var quote = NewQuote([Priced(Bearing, 1)]).Value;

        Assert.True(SalesOrder.FromQuote(OrderId.New(), quote, Today).IsSuccess);
        Assert.Equal(QuoteStatus.Accepted, quote.Status);
        Assert.Equal(QuoteErrors.NotOpen, SalesOrder.FromQuote(OrderId.New(), quote, Today).Error);
    }

    [Fact]
    public void Can_convert_on_the_last_valid_day()
    {
        var quote = NewQuote([Priced(Bearing, 1)]).Value;

        Assert.True(SalesOrder.FromQuote(OrderId.New(), quote, quote.ValidUntil).IsSuccess);
    }

    [Fact]
    public void Rejects_an_expired_quote_and_leaves_it_issued()
    {
        var quote = NewQuote([Priced(Bearing, 1)]).Value;

        Assert.Equal(QuoteErrors.Expired, SalesOrder.FromQuote(OrderId.New(), quote, quote.ValidUntil.AddDays(1)).Error);
        Assert.Equal(QuoteStatus.Issued, quote.Status);
    }

    [Fact]
    public void Rejects_a_null_quote() =>
        Assert.Throws<ArgumentNullException>(() => SalesOrder.FromQuote(OrderId.New(), null!, Today));

    [Fact]
    public void Moves_forward_from_draft_to_invoiced()
    {
        var order = SalesOrder.FromQuote(OrderId.New(), NewQuote([Priced(Bearing, 1)]).Value, Today).Value;

        Assert.True(order.Release().IsSuccess);
        Assert.Equal(OrderStatus.Released, order.Status);
        Assert.True(order.Ship().IsSuccess);
        Assert.Equal(OrderStatus.Shipped, order.Status);
        Assert.True(order.Invoice().IsSuccess);
        Assert.Equal(OrderStatus.Invoiced, order.Status);
    }

    [Fact]
    public void Refuses_to_skip_or_repeat_a_step()
    {
        var order = SalesOrder.FromQuote(OrderId.New(), NewQuote([Priced(Bearing, 1)]).Value, Today).Value;

        Assert.Equal(OrderErrors.StatusInvalidCode, order.Ship().Error.Code);
        Assert.Equal(OrderErrors.StatusInvalidCode, order.Invoice().Error.Code);
        Assert.Equal(OrderStatus.Draft, order.Status);

        order.Release();
        Assert.Equal(OrderErrors.StatusInvalid(OrderStatus.Released, OrderStatus.Released), order.Release().Error);
    }
}

public class QuoteNumberTests
{
    [Fact]
    public void Formats_year_and_sequence() =>
        Assert.Equal("Q-2026-000123", QuoteNumber.From(2026, 123).Value.ToString());

    [Theory]
    [InlineData(1999, 1)]
    [InlineData(10000, 1)]
    [InlineData(2026, 0)]
    [InlineData(2026, 1_000_000)]
    public void Rejects_out_of_range_parts(int year, int sequence) =>
        Assert.Equal(QuoteErrors.NumberInvalid, QuoteNumber.From(year, sequence).Error);

    [Theory]
    [InlineData("Q-2026-000123")]
    [InlineData(" q-2026-000123 ")]
    public void Parses_a_number(string input) =>
        Assert.Equal(QuoteNumber.From(2026, 123).Value, QuoteNumber.Parse(input).Value);

    [Theory]
    [InlineData(null)]
    [InlineData("Q-2026-123")]
    [InlineData("X-2026-000123")]
    [InlineData("Q-20A6-000123")]
    [InlineData("Q-2026_000123")]
    [InlineData("Q-2026-0001B3")]
    [InlineData("Q-1999-000123")]
    [InlineData("Q-2026-000000")]
    public void Rejects_malformed_numbers(string? input) =>
        Assert.Equal(QuoteErrors.NumberInvalid, QuoteNumber.Parse(input).Error);
}

public class AgentIdTests
{
    [Theory]
    [InlineData("acme-distributor-jolt")]
    [InlineData("a1")]
    [InlineData("nimbus-it-2")]
    public void Accepts_slugs(string input) => Assert.Equal(input, AgentId.Create(input).Value.ToString());

    [Theory]
    [InlineData(null)]
    [InlineData("a")]
    [InlineData("Acme")]
    [InlineData("1acme")]
    [InlineData("acme-")]
    [InlineData("acme--jolt")]
    [InlineData("acme_jolt")]
    public void Rejects_other_text(string? input) =>
        Assert.Equal(AgentId.InvalidCode, AgentId.Create(input).Error.Code);

    [Fact]
    public void Rejects_ids_over_64_characters()
    {
        Assert.True(AgentId.Create(new string('a', 64)).IsSuccess);
        Assert.True(AgentId.Create(new string('a', 65)).IsFailure);
    }
}
