namespace Cane360.Web.Models.Inventory;

public sealed record ConfirmInputApplicationRequest(string? LateConfirmationReason, long ExpectedVersion, string IdempotencyKey);
