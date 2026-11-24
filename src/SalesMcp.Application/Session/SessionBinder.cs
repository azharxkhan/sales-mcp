using SalesMcp.Application.Persistence;
using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Application.Session;

/// <summary>Errors from binding a session to a customer.</summary>
public static class SessionErrors
{
    /// <summary>No account number was given.</summary>
    public static readonly DomainError CustomerNotSpecified = new(
        "SessionCustomerNotSpecified",
        "No customer is bound to this session. Start the server with --customer <account number>, or set SALESMCP_CUSTOMER.");

    /// <summary>No customer has the account number.</summary>
    public static DomainError CustomerUnknown(AccountNumber account) => new(
        "SessionCustomerUnknown",
        $"No customer has account number {account}. Check the account number and start the server again.");

    /// <summary>Customers in more than one business share the account number.</summary>
    public static DomainError CustomerAmbiguous(AccountNumber account) => new(
        "SessionCustomerAmbiguous",
        $"Account number {account} exists in more than one business, so the session can't tell which customer is meant.");
}

/// <summary>
/// Finds the customer to bind a session to, from the account number given at start-up. In M3 the
/// agent profile binds the business too; until then the account number alone must be unambiguous.
/// </summary>
/// <param name="customers">Looks customers up.</param>
public sealed class SessionBinder(ICustomerReader customers)
{
    /// <summary>Resolves <paramref name="accountNumber"/> to a binding, or explains why it can't.</summary>
    public async Task<Result<SessionBinding>> ResolveAsync(string? accountNumber, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountNumber))
        {
            return Result.Failure<SessionBinding>(SessionErrors.CustomerNotSpecified);
        }

        var account = AccountNumber.Create(accountNumber);
        if (account.IsFailure)
        {
            return Result.Failure<SessionBinding>(account.Error);
        }

        var matches = await customers.FindByAccountNumberAsync(account.Value, cancellationToken).ConfigureAwait(false);
        return matches.Count switch
        {
            0 => Result.Failure<SessionBinding>(SessionErrors.CustomerUnknown(account.Value)),
            1 => Result.Success(new SessionBinding(matches[0].BusinessId, matches[0].Id, matches[0].AccountNumber)),
            _ => Result.Failure<SessionBinding>(SessionErrors.CustomerAmbiguous(account.Value)),
        };
    }
}
