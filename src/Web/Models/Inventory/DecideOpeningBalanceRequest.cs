namespace Cane360.Web.Models.Inventory;

public sealed record DecideOpeningBalanceRequest(
    long ExpectedVersion, string Outcome, string? Reason, string IdempotencyKey);
