namespace Cane360.Web.Models.Inventory;

public sealed record DecideFieldAccountabilityCorrectionRequest(
    string Outcome,
    long ExpectedVersion,
    string? Reason,
    string IdempotencyKey);
