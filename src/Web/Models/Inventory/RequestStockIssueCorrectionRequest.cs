namespace Cane360.Web.Models.Inventory;

public sealed record RequestStockIssueCorrectionRequest(long ExpectedVersion, string Reason);
