namespace Cane360.Application.Payroll;

public sealed record PayrollRunDto(Guid Id, Guid PayrollPeriodId, string PeriodName, string PeriodStatus, string Status, long Version, int LatestCalculationVersion, int? SubmittedCalculationVersion, DateTimeOffset CreatedAt, DateTimeOffset? SubmittedAt, DateTimeOffset? ApprovedAt, DateTimeOffset? RejectedAt, string? RejectionReason, DateTimeOffset? CancelledAt, string? CancellationReason, PayrollCalculationDto? Calculation, PayrollApprovalDto? Decision, string TraceId);
