using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>
/// A business buyer's account: who they are, how much credit they have and how their orders may ship.
/// </summary>
/// <remarks>
/// Credit exposure (balance + open orders + this order) is worked out in M4, when orders exist.
/// Segment (new or existing) is derived from order history, never stored; see <see cref="CustomerSegments"/>.
/// </remarks>
public sealed class Customer
{
#pragma warning disable CS8618 // Non-nullable members are set by EF Core when it loads the entity.
    /// <summary>For EF Core only.</summary>
    private Customer()
    {
    }
#pragma warning restore CS8618

    private Customer(
        CustomerId id,
        BusinessId businessId,
        AccountNumber accountNumber,
        string name,
        VatNumber? vatNumber,
        Money creditLimit,
        Money balance,
        PaymentTerms paymentTerms,
        PriceListId? priceListId,
        bool allowBackOrders,
        bool allowPartialShipment)
    {
        Id = id;
        BusinessId = businessId;
        AccountNumber = accountNumber;
        Name = name;
        VatNumber = vatNumber;
        CreditLimit = creditLimit;
        Balance = balance;
        PaymentTerms = paymentTerms;
        PriceListId = priceListId;
        AllowBackOrders = allowBackOrders;
        AllowPartialShipment = allowPartialShipment;
    }

    /// <summary>The customer's id.</summary>
    public CustomerId Id { get; }

    /// <summary>The business the customer buys from.</summary>
    public BusinessId BusinessId { get; }

    /// <summary>Human-facing account code, unique within the business.</summary>
    public AccountNumber AccountNumber { get; }

    /// <summary>Registered or trading name.</summary>
    public string Name { get; }

    /// <summary>VAT registration number, or null if the customer isn't VAT registered.</summary>
    public VatNumber? VatNumber { get; }

    /// <summary>Most the customer may owe. Zero means no credit.</summary>
    public Money CreditLimit { get; }

    /// <summary>What the customer owes now. Negative when they are in credit.</summary>
    public Money Balance { get; }

    /// <summary>How long they have to pay.</summary>
    public PaymentTerms PaymentTerms { get; }

    /// <summary>The customer's own price list, or null to use the business's list prices.</summary>
    public PriceListId? PriceListId { get; }

    /// <summary>True when lines that can't be filled from stock may become back-orders.</summary>
    public bool AllowBackOrders { get; }

    /// <summary>True when an order may ship in parts as stock becomes available.</summary>
    public bool AllowPartialShipment { get; }

    /// <summary>
    /// True when the account is held. No new orders are released while it is set. Releasing a hold is a
    /// human decision, never the AI's alone (POPIA s71); the application layer enforces who may call
    /// <see cref="ReleaseHold"/>.
    /// </summary>
    public bool OnHold { get; private set; }

    /// <summary>Validates input and creates a customer that isn't on hold.</summary>
    public static Result<Customer> Create(
        CustomerId id,
        BusinessId businessId,
        string? accountNumber,
        string? name,
        string? vatNumber,
        Money creditLimit,
        Money balance,
        PaymentTerms paymentTerms,
        PriceListId? priceListId,
        bool allowBackOrders,
        bool allowPartialShipment)
    {
        var account = AccountNumber.Create(accountNumber);
        if (account.IsFailure)
        {
            return Result.Failure<Customer>(account.Error);
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<Customer>(CustomerErrors.NameRequired);
        }

        if (trimmedName.Length > CustomerErrors.MaxNameLength)
        {
            return Result.Failure<Customer>(CustomerErrors.NameTooLong);
        }

        VatNumber? vat = null;
        if (!string.IsNullOrWhiteSpace(vatNumber))
        {
            var parsedVat = Customers.VatNumber.Create(vatNumber);
            if (parsedVat.IsFailure)
            {
                return Result.Failure<Customer>(parsedVat.Error);
            }

            vat = parsedVat.Value;
        }

        if (!Money.IsValidCurrency(creditLimit.Currency) || !Money.IsValidCurrency(balance.Currency))
        {
            return Result.Failure<Customer>(CustomerErrors.CurrencyInvalid);
        }

        if (creditLimit.Currency != balance.Currency)
        {
            return Result.Failure<Customer>(CustomerErrors.CurrencyMismatch);
        }

        if (creditLimit.IsNegative)
        {
            return Result.Failure<Customer>(CustomerErrors.CreditLimitNegative);
        }

        return Result.Success(new Customer(
            id,
            businessId,
            account.Value,
            trimmedName,
            vat,
            creditLimit,
            balance,
            paymentTerms,
            priceListId,
            allowBackOrders,
            allowPartialShipment));
    }

    /// <summary>Holds the account so no new orders are released.</summary>
    public void PlaceOnHold() => OnHold = true;

    /// <summary>Releases a hold. Only a person may decide this (see <see cref="OnHold"/>).</summary>
    public void ReleaseHold() => OnHold = false;
}
