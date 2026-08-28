namespace Cane360.Web.Models.Payroll;

public sealed record CreateWorkerAdvanceRequest(Guid WorkerId, decimal AmountUsd, string Reason, string RequestedEventDate, Guid RecoveryStartPayrollPeriodId, int? InstallmentCount, IReadOnlyList<Guid>? InstallmentPeriodIds);
