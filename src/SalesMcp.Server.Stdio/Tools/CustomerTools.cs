using System.ComponentModel;
using ModelContextProtocol.Server;
using SalesMcp.Application.Customers;
using SalesMcp.Application.Session;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Server.Stdio.Tools;

/// <summary>
/// Tools about the session's customer. Neither takes a customer argument: the customer is always
/// the one bound to the session (ADR-0016, guardrail D2).
/// </summary>
/// <param name="getCustomer">The profile use case.</param>
/// <param name="getCustomerCredit">The credit use case.</param>
/// <param name="session">The bound business and customer.</param>
[McpServerToolType]
public sealed class CustomerTools(GetCustomer getCustomer, GetCustomerCredit getCustomerCredit, ISessionContext session)
{
    /// <summary>The <c>get_customer</c> tool.</summary>
    [McpServerTool(Name = "get_customer", Title = "Get the customer", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Returns the customer you are serving in this conversation: account number, name, payment terms, and whether they accept back-orders and partial shipments.
        Use it to greet the buyer by their company name and to know their terms before quoting.
        It takes no arguments and always describes this conversation's customer; it never returns credit figures (use get_customer_credit) or any other customer.
        """)]
    public async Task<CustomerResult> GetCustomerAsync(CancellationToken cancellationToken = default)
    {
        var result = await getCustomer.ExecuteAsync(session.CustomerId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? CustomerResult.From(result.Value) : throw ToolError.From(result.Error);
    }

    /// <summary>The <c>get_customer_credit</c> tool.</summary>
    [McpServerTool(Name = "get_customer_credit", Title = "Get the customer's credit", ReadOnly = true, Idempotent = true, OpenWorld = false, UseStructuredContent = true)]
    [Description("""
        Returns this conversation's customer's credit position: credit limit, balance owed, available credit, whether they are over their limit, and whether the account is on hold, with the time the figures were read.
        Use it before suggesting a large order, or when the buyer asks about their account. Available credit is the limit minus the balance; it does not yet subtract open orders.
        It takes no arguments and never reveals another customer's figures. If the account is on hold, say a person will review any order; don't promise it will go through.
        """)]
    public async Task<CustomerCreditResult> GetCustomerCreditAsync(CancellationToken cancellationToken = default)
    {
        var result = await getCustomerCredit.ExecuteAsync(session.CustomerId, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess ? CustomerCreditResult.From(result.Value) : throw ToolError.From(result.Error);
    }
}

/// <summary>The structured result of <c>get_customer</c>.</summary>
/// <param name="AccountNumber">The account number, for example <c>KES-001</c>.</param>
/// <param name="Name">The customer's trading name.</param>
/// <param name="PaymentTerms">When invoices are due.</param>
/// <param name="AllowBackOrders">Whether items that are short can be back-ordered.</param>
/// <param name="AllowPartialShipment">Whether an order can ship in parts.</param>
public sealed record CustomerResult(
    string AccountNumber,
    string Name,
    PaymentTermsResult PaymentTerms,
    bool AllowBackOrders,
    bool AllowPartialShipment)
{
    /// <summary>Maps the use case's profile.</summary>
    public static CustomerResult From(CustomerProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new CustomerResult(
            profile.AccountNumber.Value,
            profile.Name,
            PaymentTermsResult.From(profile.PaymentTerms),
            profile.AllowBackOrders,
            profile.AllowPartialShipment);
    }
}

/// <summary>Payment terms in a tool result.</summary>
/// <param name="Basis"><c>cashWithOrder</c>, <c>daysFromInvoice</c> or <c>daysFromEndOfMonth</c>.</param>
/// <param name="Days">Days after the basis date; 0 for cash with order.</param>
/// <param name="Description">The terms in words, for example <c>30 days from end of month</c>.</param>
public sealed record PaymentTermsResult(string Basis, int Days, string Description)
{
    /// <summary>Maps domain payment terms.</summary>
    public static PaymentTermsResult From(PaymentTerms terms) => new(Wire.Name(terms.Basis), terms.Days, terms.ToString());
}

/// <summary>The structured result of <c>get_customer_credit</c>.</summary>
/// <param name="CreditLimit">The agreed credit limit.</param>
/// <param name="Balance">What the customer owes now. Negative means the business owes them.</param>
/// <param name="AvailableCredit">Credit limit minus balance, never below zero. Open orders aren't subtracted yet.</param>
/// <param name="IsOverLimit">Whether the balance is above the credit limit.</param>
/// <param name="OnHold">Whether the account is on hold, so orders wait for a person.</param>
/// <param name="AsOf">When the figures were read (UTC).</param>
public sealed record CustomerCreditResult(
    MoneyAmount CreditLimit,
    MoneyAmount Balance,
    MoneyAmount AvailableCredit,
    bool IsOverLimit,
    bool OnHold,
    DateTimeOffset AsOf)
{
    /// <summary>Maps the use case's credit position.</summary>
    public static CustomerCreditResult From(CustomerCredit credit)
    {
        ArgumentNullException.ThrowIfNull(credit);
        return new CustomerCreditResult(
            MoneyAmount.From(credit.CreditLimit),
            MoneyAmount.From(credit.Balance),
            MoneyAmount.From(credit.AvailableCredit),
            credit.IsOverLimit,
            credit.OnHold,
            credit.AsOf);
    }
}
