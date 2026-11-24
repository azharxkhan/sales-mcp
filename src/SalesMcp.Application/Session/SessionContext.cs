using SalesMcp.Domain.Common;
using SalesMcp.Domain.Customers;

namespace SalesMcp.Application.Session;

/// <summary>
/// Who this session sells to. Tools read the customer from here, never from a model argument
/// (ADR-0016, guardrail D2). There is no way to change it through this interface.
/// </summary>
public interface ISessionContext
{
    /// <summary>The business the session sells for.</summary>
    BusinessId BusinessId { get; }

    /// <summary>The customer the session sells to.</summary>
    CustomerId CustomerId { get; }

    /// <summary>The customer's account number, for messages and logs.</summary>
    AccountNumber AccountNumber { get; }
}

/// <summary>The business and customer a session is bound to.</summary>
/// <param name="BusinessId">The business.</param>
/// <param name="CustomerId">The customer.</param>
/// <param name="AccountNumber">The customer's account number.</param>
public sealed record SessionBinding(BusinessId BusinessId, CustomerId CustomerId, AccountNumber AccountNumber);

/// <summary>
/// The session context for one connection. The host binds it once, before any tool runs; it can't be
/// rebound, so a tool that got hold of this class still couldn't switch customers.
/// </summary>
public sealed class SessionContext : ISessionContext
{
    private SessionBinding? _binding;

    /// <summary>Whether a customer has been bound yet.</summary>
    public bool IsBound => _binding is not null;

    /// <inheritdoc />
    public BusinessId BusinessId => Binding.BusinessId;

    /// <inheritdoc />
    public CustomerId CustomerId => Binding.CustomerId;

    /// <inheritdoc />
    public AccountNumber AccountNumber => Binding.AccountNumber;

    private SessionBinding Binding =>
        _binding ?? throw new InvalidOperationException("No customer is bound to this session yet.");

    /// <summary>Binds the session. Only the host calls this, once, at start-up.</summary>
    /// <exception cref="InvalidOperationException">The session is already bound.</exception>
    public void Bind(SessionBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        if (Interlocked.CompareExchange(ref _binding, binding, null) is not null)
        {
            throw new InvalidOperationException("The session is already bound to a customer and can't be rebound.");
        }
    }
}
