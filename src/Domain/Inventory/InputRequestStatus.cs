namespace Cane360.Domain.Inventory;

public enum InputRequestStatus
{
    Draft,
    Submitted,
    PendingApproval,
    Approved,
    Rejected,
    Cancelled,
    PartiallyIssued,
    FullyIssued
}
