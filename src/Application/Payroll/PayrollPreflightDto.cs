namespace Cane360.Application.Payroll;

public sealed record PayrollPreflightDto(Guid PayrollPeriodId, IReadOnlyList<PreflightEvidenceDto> Evidence, int EligibleCount, int BlockedCount, int EligibleWorkerCount, int BlockedWorkerCount, int TotalCount, int Page, int PageSize, IReadOnlyList<PreflightWorkerTotalDto> WorkerTotals, IReadOnlyList<PreflightEvidenceTypeTotalDto> EvidenceTypeTotals);
