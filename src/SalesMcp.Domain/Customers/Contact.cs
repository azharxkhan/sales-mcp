using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Customers;

/// <summary>
/// A person at a customer. Personal information under POPIA: keep only what selling needs, and never
/// write it to logs.
/// </summary>
public sealed class Contact
{
    private Contact(ContactId id, CustomerId customerId, string name, string? email)
    {
        Id = id;
        CustomerId = customerId;
        Name = name;
        Email = email;
    }

    /// <summary>The contact's id.</summary>
    public ContactId Id { get; }

    /// <summary>The customer the contact works for.</summary>
    public CustomerId CustomerId { get; }

    /// <summary>Full name.</summary>
    public string Name { get; }

    /// <summary>Work email, or null if not known. Lower-cased.</summary>
    public string? Email { get; }

    /// <summary>The latest marketing decision, or null if the contact has never been asked.</summary>
    public MarketingConsent? MarketingConsent { get; private set; }

    /// <summary>
    /// True only when the latest decision is a recorded opt-in. No record, or an opt-out, means no
    /// marketing (POPIA s69).
    /// </summary>
    public bool MayReceiveMarketing => MarketingConsent is { OptedIn: true };

    /// <summary>Validates input and creates a contact with no consent record.</summary>
    public static Result<Contact> Create(ContactId id, CustomerId customerId, string? name, string? email)
    {
        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<Contact>(CustomerErrors.NameRequired);
        }

        if (trimmedName.Length > CustomerErrors.MaxNameLength)
        {
            return Result.Failure<Contact>(CustomerErrors.NameTooLong);
        }

        string? normalisedEmail = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            normalisedEmail = email.Trim().ToLowerInvariant();
            if (!IsPlausibleEmail(normalisedEmail))
            {
                return Result.Failure<Contact>(CustomerErrors.EmailInvalid);
            }
        }

        return Result.Success(new Contact(id, customerId, trimmedName, normalisedEmail));
    }

    /// <summary>Records the contact's latest marketing decision, replacing any earlier one.</summary>
    public void RecordMarketingConsent(MarketingConsent consent)
    {
        ArgumentNullException.ThrowIfNull(consent);
        MarketingConsent = consent;
    }

    // A shape check only: one '@', text either side, a dot in the domain, no whitespace.
    // Real verification is a confirmation email, which is out of scope.
    private static bool IsPlausibleEmail(string email)
    {
        if (email.Length > CustomerErrors.MaxEmailLength || email.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != email.LastIndexOf('@'))
        {
            return false;
        }

        var domain = email[(at + 1)..];
        var dot = domain.IndexOf('.', StringComparison.Ordinal);
        return dot > 0 && dot < domain.Length - 1 && !domain.EndsWith('.');
    }
}
