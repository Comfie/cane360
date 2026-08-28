namespace Cane360.Application.Payroll;

public sealed record PayrollPreflightAssessmentInput(
    bool OutsidePayrollPeriod,
    bool AbsentAttendance,
    bool MissingFieldAllocation,
    bool ConflictingFieldAllocation,
    bool MissingSupervisorAttestation,
    bool MissingManagerConfirmation,
    bool SupersededEvidence,
    bool InactiveEvidence,
    bool MissingRateSnapshot,
    bool MonthlyProrationUnresolved,
    bool DuplicateOrScopeCollision,
    bool CrossTenantOrFarmMismatch,
    bool ArchivedWorker,
    bool SourceScopeMismatch);
