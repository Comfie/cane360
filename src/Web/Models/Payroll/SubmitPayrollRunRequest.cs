namespace Cane360.Web.Models.Payroll;

public sealed record SubmitPayrollRunRequest(long ExpectedVersion, int CalculationVersion);
