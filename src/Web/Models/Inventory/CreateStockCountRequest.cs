namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockCountRequest(string EventDate, string? Notes, string CountingPersons);
