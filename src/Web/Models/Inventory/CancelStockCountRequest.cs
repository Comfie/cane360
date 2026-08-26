namespace Cane360.Web.Models.Inventory;

public sealed record CancelStockCountRequest(long ExpectedVersion, string Reason);
