namespace Cane360.Web.Models.Inventory;

public sealed record EnterStockCountLineRequest(decimal CountedQuantity, string? Notes, long ExpectedVersion);
