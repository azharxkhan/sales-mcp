using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Orders;

/// <summary>Errors raised by sales orders.</summary>
public static class OrderErrors
{
    /// <summary>Code for a status change the order's current status doesn't allow.</summary>
    public const string StatusInvalidCode = "OrderStatusInvalid";

    /// <summary>The order can't move from <paramref name="current"/> to <paramref name="target"/>.</summary>
    public static DomainError StatusInvalid(OrderStatus current, OrderStatus target) =>
        new(StatusInvalidCode, $"An order that is {current} can't become {target}.");
}
