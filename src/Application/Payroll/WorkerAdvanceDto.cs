namespace Cane360.Application.Payroll;

public sealed record WorkerAdvanceDto(Guid Id, Guid WorkerId, string WorkerName, decimal RequestedAmountUsd, decimal? ApprovedAmountUsd, string Reason, DateOnly RequestedEventDate, DateTimeOffset RequestedAt, Guid RecoveryStartPayrollPeriodId, int InstallmentCount, string Status, long Version, decimal OutstandingAmountUsd, IReadOnlyList<AdvanceInstallmentDto> Installments, IReadOnlyList<AdvanceApprovalDto> ApprovalHistory, AdvanceIssueDto? Issue);
