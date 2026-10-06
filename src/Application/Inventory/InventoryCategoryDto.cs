namespace Cane360.Application.Inventory;

public sealed record InventoryCategoryDto(
    Guid Id, string Code, string Name, string? Description, int DisplayOrder, bool Active, long Version);
