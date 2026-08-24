namespace Cane360.Web.Models.Inventory;

public sealed record ReverseStockIssueRequest(
    long ExpectedVersion, string Reason, string IdempotencyKey);
