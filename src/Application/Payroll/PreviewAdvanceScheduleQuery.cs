namespace Cane360.Application.Payroll;

public sealed record PreviewAdvanceScheduleQuery(decimal AmountUsd, Guid RecoveryStartPayrollPeriodId, int InstallmentCount) : IRequest<AdvanceSchedulePreviewDto>;
