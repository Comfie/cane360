namespace Cane360.Domain.Labour;

public enum EmploymentType
{
    Permanent,
    Seasonal,
    Casual,
    Contract,
    TaskBased
}

public enum PayBasis
{
    Daily,
    Monthly,
    Hectare,
    StandardLine
}

public enum AttendanceStatus
{
    Present,
    Absent
}

public enum WorkRecordStatus
{
    Draft,
    SupervisorVerified,
    Confirmed,
    Cancelled,
    Superseded
}

public enum WorkScopeType
{
    LineRange,
    NamedSection
}
