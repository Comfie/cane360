namespace Cane360.Web.Models.Payroll;

public sealed record PreviewAdvanceScheduleRequest(decimal AmountUsd, Guid RecoveryStartPayrollPeriodId, int InstallmentCount);
