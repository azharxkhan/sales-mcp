namespace SalesMcp.Domain.Customers;

/// <summary>Whether a customer has bought before. Derived, never stored.</summary>
public enum CustomerSegment
{
    /// <summary>No completed orders yet.</summary>
    New = 0,

    /// <summary>At least one completed order. Playbooks may treat these buyers differently (for example, account expansion).</summary>
    Existing = 1,
}

/// <summary>Derives a customer's segment from their order history.</summary>
public static class CustomerSegments
{
    /// <summary>
    /// The segment for a customer with <paramref name="completedOrderCount"/> completed orders.
    /// Derived each time so it can't drift from the order history.
    /// </summary>
    public static CustomerSegment From(int completedOrderCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(completedOrderCount);
        return completedOrderCount == 0 ? CustomerSegment.New : CustomerSegment.Existing;
    }
}
