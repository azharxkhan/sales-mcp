using SalesMcp.Application.Persistence;
using SalesMcp.Application.Session;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Application.Tests.Session;

public class SessionTests
{
    private static readonly BusinessId _acme = BusinessId.New();
    private static readonly AgentId _agent = AgentId.Create("acme-distributor-jolt").Value;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Customer NewCustomer(string account, BusinessId? business = null) =>
        Customer.Create(
            CustomerId.New(), business ?? _acme, account, "Customer " + account, null, new Money(10_000m, "ZAR"), new Money(0m, "ZAR"),
            PaymentTerms.CashWithOrder, null, allowBackOrders: true, allowPartialShipment: true).Value;

    private static SessionBinder Binder(params Customer[] customers) => new(new FakeCustomers(customers));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task No_account_number_is_a_clear_failure(string? account)
    {
        var result = await Binder(NewCustomer("KES-001")).ResolveAsync(account, _agent, Ct);

        Assert.Equal(SessionErrors.CustomerNotSpecified, result.Error);
        Assert.Contains("--customer", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_malformed_account_number_is_rejected_before_any_lookup()
    {
        var result = await Binder(NewCustomer("KES-001")).ResolveAsync("not an account!", _agent, Ct);

        Assert.Equal(CustomerErrors.AccountNumberInvalid, result.Error);
    }

    [Fact]
    public async Task An_unknown_account_number_fails_and_names_the_number()
    {
        var result = await Binder(NewCustomer("KES-001")).ResolveAsync("XYZ-999", _agent, Ct);

        Assert.Equal("SessionCustomerUnknown", result.Error.Code);
        Assert.Contains("XYZ-999", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_account_number_in_two_businesses_is_ambiguous()
    {
        var result = await Binder(NewCustomer("KES-001"), NewCustomer("KES-001", BusinessId.New())).ResolveAsync("KES-001", _agent, Ct);

        Assert.Equal("SessionCustomerAmbiguous", result.Error.Code);
    }

    [Fact]
    public async Task Binds_the_matching_customer_and_its_business_whatever_the_case()
    {
        var customer = NewCustomer("KES-001");

        var binding = (await Binder(NewCustomer("NAI-002"), customer).ResolveAsync(" kes-001 ", _agent, Ct)).Value;

        Assert.Equal(new SessionBinding(_acme, customer.Id, customer.AccountNumber, _agent), binding);
    }

    [Fact]
    public void An_unbound_context_refuses_to_answer()
    {
        var context = new SessionContext();

        Assert.False(context.IsBound);
        Assert.Throws<InvalidOperationException>(() => context.CustomerId);
        Assert.Throws<InvalidOperationException>(() => context.BusinessId);
    }

    [Fact]
    public void A_bound_context_reports_the_binding()
    {
        var customer = NewCustomer("KES-001");
        var context = new SessionContext();

        context.Bind(new SessionBinding(_acme, customer.Id, customer.AccountNumber, _agent));

        Assert.True(context.IsBound);
        Assert.Equal(_acme, context.BusinessId);
        Assert.Equal(customer.Id, context.CustomerId);
        Assert.Equal(customer.AccountNumber, context.AccountNumber);
        Assert.Equal(_agent, context.AgentId);
    }

    [Fact]
    public void A_bound_context_cannot_be_rebound_to_another_customer()
    {
        var first = NewCustomer("KES-001");
        var other = NewCustomer("NAI-002");
        var context = new SessionContext();
        context.Bind(new SessionBinding(_acme, first.Id, first.AccountNumber, _agent));

        Assert.Throws<InvalidOperationException>(() => context.Bind(new SessionBinding(_acme, other.Id, other.AccountNumber, _agent)));
        Assert.Equal(first.Id, context.CustomerId);
    }

    [Fact]
    public void Tools_see_a_read_only_interface()
    {
        // Tools depend on ISessionContext, which exposes no way to change the binding.
        var members = typeof(ISessionContext).GetMembers().Select(m => m.Name).ToList();

        Assert.DoesNotContain(members, name => name.Contains("Bind", StringComparison.Ordinal));
        Assert.All(typeof(ISessionContext).GetProperties(), p => Assert.False(p.CanWrite, p.Name));
    }

    private sealed class FakeCustomers(Customer[] customers) : ICustomerReader
    {
        public Task<Customer?> GetCustomerAsync(CustomerId id, CancellationToken cancellationToken) =>
            Task.FromResult(customers.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Contact>> ListContactsAsync(CustomerId customerId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Contact>>([]);

        public Task<IReadOnlyList<Customer>> FindByAccountNumberAsync(AccountNumber accountNumber, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>([.. customers.Where(c => c.AccountNumber == accountNumber).Take(2)]);
    }
}
