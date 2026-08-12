namespace Cane360.Domain.Activities;

public enum PersonRole
{
    FarmManager,
    Supervisor,
    Storekeeper
}

public enum ActivityPlanningKind
{
    Planned,
    Unplanned
}

public enum ActivityQuantityBasis
{
    None,
    Hectares,
    StandardLines
}

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

public enum EvidenceRole
{
    SourceSheet
}
