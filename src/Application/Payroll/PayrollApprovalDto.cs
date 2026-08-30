namespace Cane360.Application.Payroll;

public sealed record PayrollApprovalDto(Guid Id, int CalculationVersion, bool Approved, string? Reason, DateTimeOffset DecidedAt);
