namespace Cane360.Web.Models.Payroll;

public sealed record UpdateWorkerAdvanceRequest(decimal AmountUsd, string Reason, string RequestedEventDate, Guid RecoveryStartPayrollPeriodId, int InstallmentCount, long ExpectedVersion);
