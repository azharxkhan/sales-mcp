using SalesMcp.Domain.Common;

namespace SalesMcp.Application.Catalog;

/// <summary>
/// Checks a quantity a tool was given. Every tool that takes a quantity uses it, so they all accept
/// and refuse the same range (guardrail D7).
/// </summary>
public static class QuantityInput
{
    /// <summary>The largest quantity any tool accepts.</summary>
    public const decimal Max = 1_000_000m;

    /// <summary>The quantity is zero or negative.</summary>
    public static readonly DomainError NotPositive = new(
        Quantity.NotPositiveCode, "The quantity must be greater than zero.");

    /// <summary>The quantity is beyond any real order.</summary>
    public static readonly DomainError TooLarge = new(
        "QuantityTooLarge", "The quantity can be at most 1000000.");

    /// <summary>A quantity greater than 0 and at most <see cref="Max"/>, or the reason it isn't.</summary>
    /// <param name="value">The quantity as the tool received it.</param>
    public static Result<Quantity> Validate(decimal value)
    {
        if (value <= 0m)
        {
            return Result.Failure<Quantity>(NotPositive);
        }

        return value > Max
            ? Result.Failure<Quantity>(TooLarge)
            : Result.Success(new Quantity(value));
    }
}
