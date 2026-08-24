namespace Cane360.Web.Models.Inventory;

public sealed record DecideInputRequestRequest(
    long ExpectedVersion, string Outcome, string? Reason, string IdempotencyKey);
