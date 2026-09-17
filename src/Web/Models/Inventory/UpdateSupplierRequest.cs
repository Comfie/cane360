namespace Cane360.Web.Models.Inventory;

public sealed record UpdateSupplierRequest(string Code, string Name, string? Contact, long ExpectedVersion);
