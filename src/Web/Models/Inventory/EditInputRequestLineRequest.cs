namespace Cane360.Web.Models.Inventory;

public sealed record EditInputRequestLineRequest(long ExpectedVersion, decimal RequestedQuantity);
