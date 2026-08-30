namespace Cane360.Web.Models.Payroll;

public sealed record CancelPayrollRunRequest(long ExpectedVersion, string Reason);
