using System.Text.Json;

namespace Cane360.Application.Payroll;

internal static class PayrollRunMapper
{
    public static async Task<PayrollRunDto> MapAsync(IPayrollRepository repository, PayrollRun run, PayrollPeriod period, IUser user, CancellationToken cancellationToken)
    {
        var calculation = run.LatestCalculationVersion == 0 ? null : await repository.GetCalculationAsync(run.TenantId, run.FarmId, run.Id, run.LatestCalculationVersion, cancellationToken);
        var decision = await repository.GetPayrollDecisionAsync(run.TenantId, run.FarmId, run.Id, cancellationToken);
        return new PayrollRunDto(run.Id, run.PayrollPeriodId, period.DisplayName, period.Status.ToString(), run.Status.ToString(), run.Version, run.LatestCalculationVersion, run.SubmittedCalculationVersion, run.CreatedAt, run.SubmittedAt, run.ApprovedAt, run.RejectedAt, run.RejectionReason, run.CancelledAt, run.CancellationReason, calculation is null ? null : Map(calculation), decision is null ? null : new PayrollApprovalDto(decision.Id, decision.CalculationVersion, decision.Approved, decision.Reason, decision.DecidedAt), user.CorrelationId ?? string.Empty);
    }

    public static PayrollCalculationDto Map(PayrollCalculation calculation)
    {
        var blockers = JsonSerializer.Deserialize<string[]>(calculation.BlockerSnapshot) ?? [];
        return new PayrollCalculationDto(calculation.Id, calculation.CalculationVersion, calculation.CalculatedAt, calculation.GrossAmountUsd, calculation.DeductionAmountUsd, calculation.NetAmountUsd, calculation.WorkerLines.Count, calculation.EvidenceCount, blockers, blockers.Length, calculation.SourceFingerprint, calculation.WorkerLines.OrderBy(x => x.WorkerNameSnapshot).Select(MapWorker).ToArray());
    }

    private static PayrollWorkerLineDto MapWorker(PayrollWorkerLine worker) => new(worker.Id, worker.WorkerProfileId, worker.WorkerNameSnapshot, worker.GrossAmountUsd, worker.DeductionAmountUsd, worker.NetAmountUsd, worker.EarningLines.OrderBy(x => x.WorkDate).ThenBy(x => x.EvidenceId).Select(line => new PayrollEarningLineDto(line.Id, line.EvidenceId, line.EvidenceType, line.WorkDate, line.AttendanceId, line.AttendanceVersion, line.SupervisorVerifiedAtSnapshot, line.ManagerConfirmedAtSnapshot, line.FieldId, JsonSerializer.Deserialize<Guid[]>(line.ActivitySnapshot) ?? [], line.Quantity, line.Unit, line.RateType, line.RateAmountUsd, line.RateSourceId, line.RateVersion, line.EarningAmountUsd, line.SourceFingerprint)).ToArray(), worker.AdvanceDeductions.OrderBy(x => x.RecoveryPayrollPeriodId).ThenBy(x => x.WorkerAdvanceId).ThenBy(x => x.InstallmentSequence).Select(deduction => new PayrollAdvanceDeductionDto(deduction.Id, deduction.WorkerAdvanceId, deduction.AdvanceInstallmentId, deduction.RecoveryPayrollPeriodId, deduction.InstallmentSequence, deduction.ScheduledAmountUsd, deduction.OutstandingBeforeUsd, deduction.AmountUsd)).ToArray());
}
