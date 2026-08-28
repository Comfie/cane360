namespace Cane360.Application.Payroll;

public sealed record AdvanceApprovalDto(long AdvanceVersion, bool Approved, DateTimeOffset DecidedAt, string? Reason);
