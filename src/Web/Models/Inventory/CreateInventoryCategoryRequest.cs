namespace Cane360.Web.Models.Inventory;

public sealed record CreateInventoryCategoryRequest(string Code, string Name, string? Description, int DisplayOrder);
