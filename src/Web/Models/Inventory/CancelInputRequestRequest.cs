namespace Cane360.Web.Models.Inventory;

public sealed record CancelInputRequestRequest(long ExpectedVersion, string Reason);
