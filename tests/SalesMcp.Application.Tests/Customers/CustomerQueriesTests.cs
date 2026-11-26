using SalesMcp.Application.Customers;
using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Application.Tests.Customers;

public class CustomerQueriesTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly DateTimeOffset _now = new(2026, 11, 26, 8, 30, 0, TimeSpan.Zero);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Money Zar(decimal amount) => new(amount, "ZAR");

    private static Customer NewCustomer(decimal limit = 50_000m, decimal balance = 12_500m, string name = "Kestrel Ridge Mining Supplies") =>
        Customer.Create(
            CustomerId.New(), _acme, "KES-001", name, null, Zar(limit), Zar(balance),
            PaymentTerms.Create(PaymentTermsBasis.DaysFromEndOfMonth, 30).Value, null,
            allowBackOrders: true, allowPartialShipment: false).Value;

    private static async Task<CustomerCredit> CreditOf(Customer customer) =>
        (await new GetCustomerCredit(new FakeCustomers(customer), new FixedTime(_now)).ExecuteAsync(customer.Id, Ct)).Value;

    [Fact]
    public async Task The_profile_has_terms_and_shipping_flags()
    {
        var customer = NewCustomer();

        var profile = (await new GetCustomer(new FakeCustomers(customer)).ExecuteAsync(customer.Id, Ct)).Value;

        Assert.Equal(customer.AccountNumber, profile.AccountNumber);
        Assert.Equal("Kestrel Ridge Mining Supplies", profile.Name);
        Assert.Equal("30 days from end of month", profile.PaymentTerms.ToString());
        Assert.True(profile.AllowBackOrders);
        Assert.False(profile.AllowPartialShipment);
    }

    [Fact]
    public async Task The_name_is_cleaned_as_untrusted_text()
    {
        var customer = NewCustomer(name: "Kestrel​ Ridge‮ Mining");

        var profile = (await new GetCustomer(new FakeCustomers(customer)).ExecuteAsync(customer.Id, Ct)).Value;

        Assert.Equal("Kestrel Ridge Mining", profile.Name);
    }

    [Fact]
    public async Task Available_credit_is_the_limit_minus_the_balance()
    {
        var credit = await CreditOf(NewCustomer(limit: 50_000m, balance: 12_500.255m));

        Assert.Equal(Zar(50_000m), credit.CreditLimit);
        Assert.Equal(Zar(12_500.26m), credit.Balance);
        Assert.Equal(Zar(37_499.75m), credit.AvailableCredit);
        Assert.False(credit.IsOverLimit);
        Assert.Equal(_now, credit.AsOf);
    }

    [Fact]
    public async Task A_credit_balance_adds_to_available_credit()
    {
        var credit = await CreditOf(NewCustomer(limit: 10_000m, balance: -500m));

        Assert.Equal(Zar(10_500m), credit.AvailableCredit);
    }

    [Fact]
    public async Task Over_the_limit_means_no_available_credit_and_says_so()
    {
        var credit = await CreditOf(NewCustomer(limit: 10_000m, balance: 10_250m));

        Assert.Equal(Zar(0m), credit.AvailableCredit);
        Assert.True(credit.IsOverLimit);
    }

    [Fact]
    public async Task Reports_a_hold()
    {
        var customer = NewCustomer();
        customer.PlaceOnHold();

        Assert.True((await CreditOf(customer)).OnHold);
    }

    [Fact]
    public async Task A_missing_customer_is_a_clear_failure()
    {
        var reader = new FakeCustomers();

        Assert.Equal(CustomerQueryErrors.CustomerNotFound, (await new GetCustomer(reader).ExecuteAsync(CustomerId.New(), Ct)).Error);
        Assert.Equal(CustomerQueryErrors.CustomerNotFound, (await new GetCustomerCredit(reader, new FixedTime(_now)).ExecuteAsync(CustomerId.New(), Ct)).Error);
    }

    private sealed class FixedTime(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeCustomers(params Customer[] customers) : ICustomerReader
    {
        public Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(customers.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Contact>>([]);

        public Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>([.. customers.Where(c => c.AccountNumber == accountNumber)]);
    }
}
