namespace Cane360.Application.Payroll;

public static class PayrollPreflightAssessment
{
    public static IReadOnlyList<PayrollPreflightBlocker> Assess(PayrollPreflightAssessmentInput input)
    {
        var blockers = new List<PayrollPreflightBlocker>();

        Add(input.OutsidePayrollPeriod, PayrollPreflightBlockerCodes.OutsidePayrollPeriod, "The evidence date is outside this payroll period.");
        Add(input.AbsentAttendance, PayrollPreflightBlockerCodes.AbsentAttendance, "Present attendance is required for this worker and day.");
        Add(input.MissingFieldAllocation, PayrollPreflightBlockerCodes.MissingFieldAllocation, "Present attendance must contain one field allocation.");
        Add(input.ConflictingFieldAllocation, PayrollPreflightBlockerCodes.ConflictingFieldAllocation, "The attendance field conflicts with the work evidence field.");
        Add(input.MissingSupervisorAttestation, PayrollPreflightBlockerCodes.MissingSupervisorAttestation, "Supervisor attestation is required.");
        Add(input.MissingManagerConfirmation, PayrollPreflightBlockerCodes.MissingManagerConfirmation, "Manager confirmation is required.");
        Add(input.SupersededEvidence, PayrollPreflightBlockerCodes.SupersededEvidence, "This evidence was superseded by an append-only correction.");
        Add(input.InactiveEvidence, PayrollPreflightBlockerCodes.InactiveEvidence, "This evidence is cancelled and inactive.");
        Add(input.MissingRateSnapshot, PayrollPreflightBlockerCodes.MissingRateSnapshot, "A positive event-date rate snapshot is required.");
        Add(input.MonthlyProrationUnresolved, PayrollPreflightBlockerCodes.MonthlyProrationNotConfigured, "Monthly evidence cannot be calculated because no workday or proration policy is configured.");
        Add(input.DuplicateOrScopeCollision, PayrollPreflightBlockerCodes.DuplicateOrScopeCollision, "Duplicate active evidence or an unresolved work-scope collision exists for this worker, day, and activity.");
        Add(input.CrossTenantOrFarmMismatch, PayrollPreflightBlockerCodes.CrossTenantOrFarmMismatch, "A worker, attendance, or evidence source is outside the authenticated tenant and farm.");
        Add(input.ArchivedWorker, PayrollPreflightBlockerCodes.ArchivedWorker, "Historical evidence remains visible, but archived workers cannot enter new payroll processing.");
        Add(input.SourceScopeMismatch, PayrollPreflightBlockerCodes.SourceScopeMismatch, "The farm, field, crop cycle, or activity source is inactive or outside the evidence scope.");

        return blockers;

        void Add(bool blocked, string code, string explanation)
        {
            if (blocked)
                blockers.Add(new PayrollPreflightBlocker(code, explanation));
        }
    }
}
