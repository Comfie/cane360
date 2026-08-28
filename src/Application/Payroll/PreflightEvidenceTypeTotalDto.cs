namespace Cane360.Application.Payroll;

public sealed record PreflightEvidenceTypeTotalDto(string EvidenceType, int EligibleCount, int BlockedCount);
