using SalesMcp.Application.Catalog;
using SalesMcp.Application.Customers;
using SalesMcp.Application.Persistence;
using SalesMcp.Application.Quotes;
using SalesMcp.Application.Requests;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;
using SalesMcp.Domain.Pricing;
using SalesMcp.Domain.Quotes;
using SalesMcp.Domain.Requests;
using SalesMcp.Domain.Tax;

namespace SalesMcp.Application.Tests.Quotes;

public class CreateQuoteTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly AgentId _agent = AgentId.Create("acme-distributor-jolt").Value;
    private static readonly DateTimeOffset _now = new(2026, 11, 28, 9, 0, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private static Product NewProduct(string sku, VatCode vat = VatCode.Standard) =>
        Product.Create(OfferingId.New(), _acme, sku, "Item " + sku, "", vat, UnitOfMeasure.Box, 3, 30).Value;

    private static Customer NewCustomer(BusinessId? business = null) =>
        Customer.Create(
            CustomerId.New(), business ?? _acme, "KES-001", "Kestrel Ridge", null, Zar(50_000m), Zar(0m),
            PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30).Value, null,
            allowBackOrders: true, allowPartialShipment: true).Value;

    private static QuoteLineRequest?[] Lines(params (string Offering, decimal Quantity)[] lines) =>
        [.. lines.Select(l => new QuoteLineRequest(l.Offering, l.Quantity))];

    [Fact]
    public async Task Issues_a_numbered_quote_priced_by_the_resolver()
    {
        var world = new World();

        var issued = (await world.Create(Lines(("ACM-FAS-101", 3m), ("ACM-EXM-001", 1m)), "req-0001")).Value;

        var quote = issued.Quote;
        Assert.Equal("Q-2026-000001", quote.Number.Value);
        Assert.Equal(new DateOnly(2026, 11, 28), quote.IssuedOn);
        Assert.Equal(new DateOnly(2026, 12, 28), quote.ValidUntil);
        Assert.Equal(_agent, quote.AgentId);
        Assert.Equal(world.Customer.Id, quote.CustomerId);
        Assert.Equal([Zar(100m), Zar(40m)], quote.Lines.Select(l => l.Price.UnitPrice));
        Assert.Equal(Zar(345m), quote.Lines[0].Total);
        Assert.Equal(Zar(385m), quote.Totals.Total);
        Assert.All(quote.Lines, l => Assert.True(l.Price.Discount.IsZero));
        Assert.Equal(UnitOfMeasure.Box, issued.Lines[0].Unit);
        Assert.Single(world.Documents.Quotes);
        Assert.True(world.Documents.Committed);
    }

    [Fact]
    public async Task The_document_is_the_offer_with_its_number_validity_and_terms()
    {
        var world = new World();

        var document = (await world.Create(Lines(("ACM-FAS-101", 3m)), "req-0001")).Value.Document;

        Assert.Contains("# Quote Q-2026-000001", document, StringComparison.Ordinal);
        Assert.Contains("**Valid until:** 2026-12-28", document, StringComparison.Ordinal);
        Assert.Contains(QuoteDocument.OfferStatement, document, StringComparison.Ordinal);
        Assert.Contains("Payment: 30 days from end of month.", document, StringComparison.Ordinal);
        Assert.Contains("Kestrel Ridge (account KES-001)", document, StringComparison.Ordinal);
        Assert.Contains("| 1 | ACM-FAS-101 | Item ACM-FAS-101 | 3 box | ZAR 100.00 | ZAR 300.00 | VAT 15% ZAR 45.00 | ZAR 345.00 |", document, StringComparison.Ordinal);
        Assert.Contains("**Total:** ZAR 345.00", document, StringComparison.Ordinal);
        Assert.Contains("AI sales agent acme-distributor-jolt", document, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Numbers_follow_on_without_gaps()
    {
        var world = new World();

        var first = (await world.Create(Lines(("ACM-FAS-101", 1m)), "req-0001")).Value;
        await world.Create(Lines(("ACM-NOPE-999", 1m)), "req-0002");
        var second = (await world.Create(Lines(("ACM-FAS-101", 2m)), "req-0003")).Value;

        Assert.Equal("Q-2026-000001", first.Quote.Number.Value);
        Assert.Equal("Q-2026-000002", second.Quote.Number.Value);
    }

    [Fact]
    public async Task The_same_request_id_and_lines_return_the_first_quote()
    {
        var world = new World();

        var first = (await world.Create(Lines(("ACM-FAS-101", 2m)), "req-0001")).Value;
        world.Time.Advance(TimeSpan.FromHours(1));
        var retry = (await world.Create(Lines((" acm-fas-101 ", 2.0m)), "req-0001")).Value;

        Assert.Equal(first.Quote.Id, retry.Quote.Id);
        Assert.Equal(first.Document, retry.Document);
        Assert.Single(world.Documents.Quotes);
    }

    [Fact]
    public async Task The_same_request_id_with_other_lines_conflicts()
    {
        var world = new World();
        await world.Create(Lines(("ACM-FAS-101", 2m)), "req-0001");

        var result = await world.Create(Lines(("ACM-FAS-101", 3m)), "req-0001");

        Assert.Equal(IdempotencyErrors.Conflict, result.Error);
        Assert.Single(world.Documents.Quotes);
    }

    [Fact]
    public async Task After_24_hours_a_request_id_issues_a_new_quote()
    {
        var world = new World();
        var first = (await world.Create(Lines(("ACM-FAS-101", 2m)), "req-0001")).Value;

        world.Time.Advance(IdempotencyRecord.Lifetime);
        var later = (await world.Create(Lines(("ACM-FAS-101", 3m)), "req-0001")).Value;

        Assert.NotEqual(first.Quote.Id, later.Quote.Id);
        Assert.Equal("Q-2026-000002", later.Quote.Number.Value);
        Assert.Single(world.Documents.Records);
    }

    [Fact]
    public async Task A_request_id_is_scoped_to_the_customer()
    {
        var world = new World();
        var other = NewCustomer();
        world.Customers.Add(other);

        var mine = (await world.Create(Lines(("ACM-FAS-101", 2m)), "req-0001")).Value;
        var theirs = (await world.CreateFor(other, Lines(("ACM-FAS-101", 5m)), "req-0001")).Value;

        Assert.NotEqual(mine.Quote.Id, theirs.Quote.Id);
        Assert.Equal(other.Id, theirs.Quote.CustomerId);
    }

    [Theory]
    [InlineData(null, "RequestIdRequired")]
    [InlineData("  ", "RequestIdRequired")]
    [InlineData("short", "RequestIdInvalid")]
    [InlineData("has spaces in it", "RequestIdInvalid")]
    public async Task A_missing_or_malformed_request_id_is_refused(string? requestId, string code)
    {
        var world = new World();

        Assert.Equal(code, (await world.Create(Lines(("ACM-FAS-101", 1m)), requestId)).Error.Code);
        Assert.Empty(world.Documents.Quotes);
    }

    [Fact]
    public async Task No_lines_or_too_many_lines_are_refused()
    {
        var world = new World();
        var tooMany = Enumerable.Range(1, CreateQuoteErrors.MaxLines + 1).Select(i => ("ACM-FAS-101", (decimal)i)).ToArray();

        Assert.Equal(QuoteErrors.Empty, (await world.Create([], "req-0001")).Error);
        Assert.Equal(QuoteErrors.Empty, (await world.Create(null, "req-0002")).Error);
        Assert.Equal(CreateQuoteErrors.TooManyLines, (await world.Create(Lines(tooMany), "req-0003")).Error);
    }

    [Theory]
    [InlineData(0, "QuantityNotPositive")]
    [InlineData(-1, "QuantityNotPositive")]
    [InlineData(1_000_001, "QuantityTooLarge")]
    public async Task A_bad_quantity_names_its_line(int quantity, string code)
    {
        var world = new World();

        var error = (await world.Create(Lines(("ACM-FAS-101", 1m), ("ACM-EXM-001", quantity)), "req-0001")).Error;

        Assert.Equal(code, error.Code);
        Assert.StartsWith("Line 2: ", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unknown_inactive_duplicate_and_unpriced_lines_issue_nothing()
    {
        var world = new World();
        world.Offerings.Add(NewProduct("ACM-OLD-001"));
        world.Offerings[^1].Deactivate();
        world.Offerings.Add(NewProduct("ACM-UNL-001"));

        Assert.Equal("OfferingNotFound", (await world.Create(Lines(("ACM-NOPE-999", 1m)), "req-0001")).Error.Code);
        Assert.Equal("OfferingInactive", (await world.Create(Lines(("ACM-OLD-001", 1m)), "req-0002")).Error.Code);
        Assert.Equal(QuoteErrors.LineDuplicate, (await world.Create(Lines(("ACM-FAS-101", 1m), ("acm-fas-101", 2m)), "req-0003")).Error);
        Assert.Equal(PricingErrors.NoPrice.Code, (await world.Create(Lines(("ACM-UNL-001", 1m)), "req-0004")).Error.Code);
        Assert.Equal("OfferingRequired", (await world.Create([null], "req-0005")).Error.Code);
        Assert.Empty(world.Documents.Quotes);
        Assert.Empty(world.Documents.Records);
    }

    [Fact]
    public async Task A_customer_from_another_business_is_not_found()
    {
        var world = new World();
        var stranger = NewCustomer(BusinessId.New());
        world.Customers.Add(stranger);

        Assert.Equal(CustomerQueryErrors.CustomerNotFound, (await world.CreateFor(stranger, Lines(("ACM-FAS-101", 1m)), "req-0001")).Error);
    }

    [Fact]
    public async Task Names_and_step_details_are_cleaned()
    {
        var world = new World(listName: "Acme​ list‮ prices");

        var line = (await world.Create(Lines(("ACM-FAS-101", 1m)), "req-0001")).Value.Lines[0];

        Assert.Equal("Acme list prices, 1+ break", line.Steps[0].Detail);
    }

    /// <summary>A business with two priced products, one customer, and in-memory storage.</summary>
    private sealed class World
    {
        public World(string listName = "Acme list prices")
        {
            var bolts = NewProduct("ACM-FAS-101");
            var exempt = NewProduct("ACM-EXM-001", VatCode.Exempt);
            Offerings = [bolts, exempt];
            Customer = NewCustomer();
            Customers = [Customer];

            List = PriceList.Create(PriceListId.New(), _acme, listName, "ZAR", isDefault: true).Value;
            List.AddItem(bolts.Id, new Quantity(1m), Zar(100m));
            List.AddItem(exempt.Id, new Quantity(1m), Zar(40m));
        }

        public List<Offering> Offerings { get; }

        public Customer Customer { get; }

        public List<Customer> Customers { get; }

        public PriceList List { get; }

        public MovableTime Time { get; } = new(_now);

        public FakeDocuments Documents { get; } = new();

        public Task<Result<IssuedQuote>> Create(IReadOnlyList<QuoteLineRequest?>? lines, string? requestId) =>
            CreateFor(Customer, lines, requestId);

        public Task<Result<IssuedQuote>> CreateFor(Customer customer, IReadOnlyList<QuoteLineRequest?>? lines, string? requestId)
        {
            var catalog = new FakeCatalog(Offerings);
            var useCase = new CreateQuote(
                new OfferingLookup(catalog),
                new FakeCustomers(Customers),
                new FakePricing(new PriceSources(List, null, [])),
                new PriceResolver(VatRateTable.SouthAfrica),
                Documents,
                Documents,
                Documents,
                Documents,
                Time,
                new QuotePresenter(new FakeBusinesses(), catalog));
            return useCase.ExecuteAsync(_acme, customer.Id, _agent, lines, requestId, Ct);
        }
    }

    private sealed class MovableTime(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan by) => _now += by;
    }

    /// <summary>Storage that only keeps what a committed transaction saved.</summary>
    private sealed class FakeDocuments : IQuoteRepository, IQuoteNumbers, IIdempotencyStore, IUnitOfWork, IWriteTransaction
    {
        private readonly List<Quote> _pendingQuotes = [];
        private readonly List<IdempotencyRecord> _pendingRecords = [];
        private bool _saved;

        public List<Quote> Quotes { get; } = [];

        public List<IdempotencyRecord> Records { get; } = [];

        public bool Committed { get; private set; }

        public Task<Quote?> GetAsync(QuoteId id, CancellationToken cancellationToken) =>
            Task.FromResult(Quotes.FirstOrDefault(q => q.Id == id));

        public Task<Quote?> FindByNumberAsync(CustomerId customerId, QuoteNumber number, CancellationToken cancellationToken) =>
            Task.FromResult(Quotes.FirstOrDefault(q => q.Number == number && q.CustomerId == customerId));

        public void Add(Quote quote) => _pendingQuotes.Add(quote);

        public Task<Result<QuoteNumber>> NextAsync(int year, CancellationToken cancellationToken)
        {
            var last = Quotes.Where(q => q.IssuedOn.Year == year).Select(q => int.Parse(q.Number.Value[7..], System.Globalization.CultureInfo.InvariantCulture)).DefaultIfEmpty(0).Max();
            return Task.FromResult(QuoteNumber.From(year, last + 1));
        }

        public Task<IdempotencyRecord?> FindAsync(string key, string tool, CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult(Records.FirstOrDefault(r => r.Key == key && r.Tool == tool && r.CustomerId == customerId));

        public void Add(IdempotencyRecord record) => _pendingRecords.Add(record);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            _saved = true;
            return Task.CompletedTask;
        }

        public Task<IWriteTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
        {
            _saved = false;
            _pendingQuotes.Clear();
            _pendingRecords.Clear();
            return Task.FromResult<IWriteTransaction>(this);
        }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            Assert.True(_saved, "Committed before saving.");
            Quotes.AddRange(_pendingQuotes);
            Records.AddRange(_pendingRecords);
            Committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FakeBusinesses : IBusinessReader
    {
        public Task<Business?> GetBusinessAsync(BusinessId id, CancellationToken cancellationToken) =>
            Task.FromResult<Business?>(Business.Create(id, "Acme Distributors", "ZAR", "en-ZA").Value);
    }

    private sealed class FakeCatalog(List<Offering> offerings) : ICatalogReader
    {
        public Task<Offering?> GetOfferingAsync(OfferingId id, CancellationToken cancellationToken) =>
            Task.FromResult(offerings.FirstOrDefault(o => o.Id == id));

        public Task<IReadOnlyList<Offering>> ListActiveOfferingsAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Offering>>([.. offerings.Where(o => o.IsActive)]);

        public Task<Offering?> FindBySkuAsync(BusinessId businessId, Sku sku, CancellationToken cancellationToken) =>
            Task.FromResult(offerings.FirstOrDefault(o => o.BusinessId == businessId && o.Sku == sku));

        public Task<OfferingRuleSet> GetRulesAsync(BusinessId businessId, CancellationToken cancellationToken) =>
            Task.FromResult(new OfferingRuleSet());
    }

    private sealed class FakeCustomers(List<Customer> customers) : ICustomerReader
    {
        public Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(customers.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Contact>>([]);

        public Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>([.. customers.Where(c => c.AccountNumber == accountNumber)]);
    }

    private sealed class FakePricing(PriceSources sources) : IPricingReader
    {
        public Task<PriceSources?> GetPriceSourcesAsync(Customer customer, CancellationToken cancellationToken) =>
            Task.FromResult<PriceSources?>(sources);
    }
}
