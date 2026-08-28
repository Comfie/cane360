namespace Cane360.Web.Models.Payroll;

public sealed record DecideWorkerAdvanceRequest(long ExpectedVersion, bool Approved, string? Reason, string IdempotencyKey);
