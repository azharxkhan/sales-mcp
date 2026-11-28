using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Orders;

/// <summary>Errors raised by sales orders.</summary>
public static class OrderErrors
{
    /// <summary>The order number doesn't match the format.</summary>
    public static DomainError NumberInvalid { get; } =
        new("OrderNumberInvalid", "An order number looks like SO-2026-000123.");

    /// <summary>Code for a status change the order's current status doesn't allow.</summary>
    public const string StatusInvalidCode = "OrderStatusInvalid";

    /// <summary>The order can't move from <paramref name="current"/> to <paramref name="target"/>.</summary>
    public static DomainError StatusInvalid(OrderStatus current, OrderStatus target) =>
        new(StatusInvalidCode, $"An order that is {current} can't become {target}.");
}
