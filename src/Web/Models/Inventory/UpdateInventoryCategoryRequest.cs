namespace Cane360.Web.Models.Inventory;

public sealed record UpdateInventoryCategoryRequest(string Name, string? Description, int DisplayOrder, long ExpectedVersion);
