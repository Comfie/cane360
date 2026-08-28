namespace Cane360.Application.Payroll;

public static class PayrollPreflightBlockerCodes
{
    public const string AbsentAttendance = "ABSENT_ATTENDANCE";
    public const string MissingFieldAllocation = "MISSING_FIELD_ALLOCATION";
    public const string ConflictingFieldAllocation = "CONFLICTING_FIELD_ALLOCATION";
    public const string MissingSupervisorAttestation = "MISSING_SUPERVISOR_ATTESTATION";
    public const string MissingManagerConfirmation = "MISSING_MANAGER_CONFIRMATION";
    public const string SupersededEvidence = "SUPERSEDED_EVIDENCE";
    public const string InactiveEvidence = "INACTIVE_EVIDENCE";
    public const string DuplicateOrScopeCollision = "DUPLICATE_EVIDENCE_OR_SCOPE_COLLISION";
    public const string OutsidePayrollPeriod = "OUTSIDE_PAYROLL_PERIOD";
    public const string CrossTenantOrFarmMismatch = "CROSS_TENANT_OR_FARM_MISMATCH";
    public const string MissingRateSnapshot = "MISSING_RATE_SNAPSHOT";
    public const string ArchivedWorker = "ARCHIVED_WORKER";
    public const string MonthlyProrationUnresolved = "MONTHLY_PRORATION_UNRESOLVED";
    public const string SourceScopeMismatch = "SOURCE_SCOPE_MISMATCH";
}
