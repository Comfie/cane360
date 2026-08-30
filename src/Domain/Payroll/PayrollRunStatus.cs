namespace Cane360.Domain.Payroll;

public enum PayrollRunStatus
{
    Draft,
    Calculated,
    PendingGrowerApproval,
    Approved,
    Rejected,
    Cancelled
}
