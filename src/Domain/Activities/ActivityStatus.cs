namespace Cane360.Domain.Activities;

public enum ActivityStatus
{
    Draft,
    Planned,
    InProgress,
    AwaitingVerification,
    ManagerConfirmation,
    Completed,
    Closed,
    Cancelled
}
