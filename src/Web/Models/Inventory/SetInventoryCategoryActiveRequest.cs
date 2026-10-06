namespace Cane360.Web.Models.Inventory;

public sealed record SetInventoryCategoryActiveRequest(bool Active, long ExpectedVersion);
