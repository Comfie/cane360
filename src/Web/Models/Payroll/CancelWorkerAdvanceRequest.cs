namespace Cane360.Web.Models.Payroll;

public sealed record CancelWorkerAdvanceRequest(long ExpectedVersion, string Reason);
