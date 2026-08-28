namespace Cane360.Web.Models.Payroll;

public sealed record CancelPayrollPeriodRequest(long ExpectedVersion, string Reason);
