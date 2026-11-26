using SalesMcp.Application.Persistence;
using SalesMcp.Application.Text;
using SalesMcp.Domain.Catalog;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Application.Customers;

/// <summary>Who the session's customer is and how they buy. No credit figures (guardrail E2).</summary>
/// <param name="AccountNumber">The account number.</param>
/// <param name="Name">The trading name, cleaned as untrusted text.</param>
/// <param name="PaymentTerms">When invoices are due.</param>
/// <param name="AllowBackOrders">Whether a shortfall may be back-ordered.</param>
/// <param name="AllowPartialShipment">Whether an order may ship in parts.</param>
public sealed record CustomerProfile(
    AccountNumber AccountNumber,
    string Name,
    PaymentTerms PaymentTerms,
    bool AllowBackOrders,
    bool AllowPartialShipment);

/// <summary>The session's customer's credit position.</summary>
/// <param name="CreditLimit">The agreed credit limit.</param>
/// <param name="Balance">What the customer owes now. Negative means the business owes them.</param>
/// <param name="AvailableCredit">Credit limit minus balance, never below zero.</param>
/// <param name="IsOverLimit">Whether the balance is above the credit limit.</param>
/// <param name="OnHold">Whether the account is on hold, so new orders wait for a person.</param>
/// <param name="AsOf">When these figures were read.</param>
public sealed record CustomerCredit(
    Money CreditLimit,
    Money Balance,
    Money AvailableCredit,
    bool IsOverLimit,
    bool OnHold,
    DateTimeOffset AsOf);

/// <summary>Errors from the customer use cases.</summary>
public static class CustomerQueryErrors
{
    /// <summary>The bound customer no longer exists.</summary>
    public static readonly DomainError CustomerNotFound = new(
        "CustomerNotFound", "This session's customer account can't be found. Ask a person to check the account.");
}

/// <summary>Reads the bound customer's profile.</summary>
/// <param name="customers">Reads customers.</param>
public sealed class GetCustomer(ICustomerReader customers)
{
    /// <summary>The profile of <paramref name="customerId"/>, which must be the session's customer.</summary>
    public async Task<Result<CustomerProfile>> ExecuteAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        return customer is null
            ? Result.Failure<CustomerProfile>(CustomerQueryErrors.CustomerNotFound)
            : Result.Success(new CustomerProfile(
                customer.AccountNumber,
                UntrustedText.Clean(customer.Name, CatalogErrors.MaxNameLength),
                customer.PaymentTerms,
                customer.AllowBackOrders,
                customer.AllowPartialShipment));
    }
}

/// <summary>
/// Reads the bound customer's credit. Available credit is the limit minus the balance. It doesn't
/// yet count open orders: full credit exposure arrives with orders and holds in M4.
/// </summary>
/// <param name="customers">Reads customers.</param>
/// <param name="time">The clock for <see cref="CustomerCredit.AsOf"/>.</param>
public sealed class GetCustomerCredit(ICustomerReader customers, TimeProvider time)
{
    /// <summary>The credit position of <paramref name="customerId"/>, which must be the session's customer.</summary>
    public async Task<Result<CustomerCredit>> ExecuteAsync(CustomerId customerId, CancellationToken cancellationToken)
    {
        var customer = await customers.GetCustomerAsync(customerId, cancellationToken).ConfigureAwait(false);
        if (customer is null)
        {
            return Result.Failure<CustomerCredit>(CustomerQueryErrors.CustomerNotFound);
        }

        var headroom = (customer.CreditLimit - customer.Balance).Round();
        var available = headroom.IsNegative ? Money.Zero(headroom.Currency) : headroom;
        return Result.Success(new CustomerCredit(
            customer.CreditLimit.Round(),
            customer.Balance.Round(),
            available,
            headroom.IsNegative,
            customer.OnHold,
            time.GetUtcNow()));
    }
}
