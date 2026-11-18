using SalesMcp.Domain.Common;

namespace SalesMcp.Domain.Inventory;

/// <summary>A place a business holds stock, such as its Durban branch.</summary>
public sealed class Warehouse
{
    private Warehouse(WarehouseId id, BusinessId businessId, string code, string name)
    {
        Id = id;
        BusinessId = businessId;
        Code = code;
        Name = name;
    }

    /// <summary>The warehouse's id.</summary>
    public WarehouseId Id { get; }

    /// <summary>The business that owns it.</summary>
    public BusinessId BusinessId { get; }

    /// <summary>Short upper-case code, unique within the business, for example <c>DBN</c>.</summary>
    public string Code { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>Validates and creates a warehouse. The code is trimmed and upper-cased.</summary>
    public static Result<Warehouse> Create(WarehouseId id, BusinessId businessId, string? code, string? name)
    {
        var normalisedCode = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalisedCode.Length is < 2 or > 10 || !normalisedCode.All(char.IsAsciiLetterOrDigit))
        {
            return Result.Failure<Warehouse>(InventoryErrors.WarehouseCodeInvalid);
        }

        var trimmedName = name?.Trim() ?? string.Empty;
        if (trimmedName.Length == 0)
        {
            return Result.Failure<Warehouse>(InventoryErrors.NameRequired);
        }

        return trimmedName.Length > InventoryErrors.MaxNameLength
            ? Result.Failure<Warehouse>(InventoryErrors.NameTooLong)
            : Result.Success(new Warehouse(id, businessId, normalisedCode, trimmedName));
    }
}
