using System.Text.Json;

namespace Cane360.Application.Payroll;

public sealed class DecidePayrollRunCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<DecidePayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(DecidePayrollRunCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); PayrollAccess.RequireGrower(tenant, userId);
        await using var transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        var existing = await payroll.GetPayrollApprovalByKeyAsync(tenant.Id, farm.Id, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        { if (existing.PayrollRunId != request.PayrollRunId || existing.RunVersion != request.ExpectedVersion || existing.CalculationVersion != request.CalculationVersion || existing.Approved != request.Approved) throw new ConflictException("This payroll decision idempotency key is bound to a different exact version or outcome."); var prior = PayrollAccess.RequireRun(await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, false, cancellationToken), request.PayrollRunId); var priorPeriod = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, prior.PayrollPeriodId, false, cancellationToken), prior.PayrollPeriodId); var result = await PayrollRunMapper.MapAsync(payroll, prior, priorPeriod, user, cancellationToken); await transaction.CommitAsync(cancellationToken); return result; }
        var run = PayrollAccess.RequireRun(await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, true, cancellationToken), request.PayrollRunId); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, true, cancellationToken), run.PayrollPeriodId); var calculation = await payroll.GetCalculationAsync(tenant.Id, farm.Id, run.Id, request.CalculationVersion, cancellationToken) ?? throw new NotFoundException(request.CalculationVersion.ToString(), "Payroll calculation");
        if (run.Version != request.ExpectedVersion) throw new ConflictException("This payroll run changed after it was loaded. Refresh and try again.");
        if (run.Status != PayrollRunStatus.PendingGrowerApproval || run.SubmittedCalculationVersion != request.CalculationVersion) throw new ConflictException("The exact submitted payroll calculation version is no longer pending Grower approval.");
        var now = clock.GetUtcNow();
        if (request.Approved)
        {
            var fresh = await PayrollCalculationBuilder.BuildAsync(farms, labour, payroll, tenant, farm, period, run, request.CalculationVersion, now, userId, PayrollAccess.OperationalPerson(tenant, userId), cancellationToken);
            var freshBlockers = JsonSerializer.Deserialize<string[]>(fresh.BlockerSnapshot) ?? [];
            var staleCodes = freshBlockers.ToList(); var originalLines = calculation.WorkerLines.SelectMany(x => x.EarningLines).ToDictionary(x => x.EvidenceId); var freshLines = fresh.WorkerLines.SelectMany(x => x.EarningLines).ToDictionary(x => x.EvidenceId);
            if (!originalLines.Keys.Order().SequenceEqual(freshLines.Keys.Order())) staleCodes.Add(PayrollPreflightBlockerCodes.EvidenceChangedAfterCalculation);
            foreach (var pair in originalLines.Where(x => freshLines.ContainsKey(x.Key)))
            {
                var next = freshLines[pair.Key];
                if (pair.Value.SourceFingerprint != next.SourceFingerprint || pair.Value.AttendanceVersion != next.AttendanceVersion) staleCodes.Add(PayrollPreflightBlockerCodes.EvidenceChangedAfterCalculation);
                if (pair.Value.SupervisorVerifiedAtSnapshot != next.SupervisorVerifiedAtSnapshot || pair.Value.ManagerConfirmedAtSnapshot != next.ManagerConfirmedAtSnapshot) staleCodes.Add(PayrollPreflightBlockerCodes.VerificationChanged);
                if (pair.Value.RateSourceId != next.RateSourceId || pair.Value.RateVersion != next.RateVersion || pair.Value.RateAmountUsd != next.RateAmountUsd) staleCodes.Add(PayrollPreflightBlockerCodes.RateSnapshotChanged);
            }
            var originalDeductions = calculation.WorkerLines.SelectMany(x => x.AdvanceDeductions).Select(x => $"{x.WorkerAdvanceId:N}:{x.AdvanceInstallmentId:N}:{x.OutstandingBeforeUsd}:{x.AmountUsd}").Order().ToArray(); var freshDeductions = fresh.WorkerLines.SelectMany(x => x.AdvanceDeductions).Select(x => $"{x.WorkerAdvanceId:N}:{x.AdvanceInstallmentId:N}:{x.OutstandingBeforeUsd}:{x.AmountUsd}").Order().ToArray();
            if (!originalDeductions.SequenceEqual(freshDeductions)) staleCodes.Add(PayrollPreflightBlockerCodes.AdvanceChangedAfterCalculation);
            if (fresh.SourceFingerprint != calculation.SourceFingerprint || fresh.GrossAmountUsd != calculation.GrossAmountUsd || fresh.DeductionAmountUsd != calculation.DeductionAmountUsd || fresh.NetAmountUsd != calculation.NetAmountUsd || staleCodes.Count != 0)
            { staleCodes.Add(PayrollPreflightBlockerCodes.PayrollCalculationStale); throw new ConflictException($"PayrollCalculationStale: authoritative payroll sources changed after calculation. Recalculate and resubmit. Details: {string.Join(", ", staleCodes.Distinct())}"); }
        }
        var subjectVersion = run.Version; PayrollAccess.Domain(() => run.Decide(request.Approved, request.CalculationVersion, now, request.Reason, request.ExpectedVersion), nameof(request.ExpectedVersion));
        var approval = PayrollApproval.Create(run.Id, calculation.Id, tenant.Id, farm.Id, subjectVersion, request.CalculationVersion, request.Approved, request.Reason, now, userId, PayrollAccess.OperationalPerson(tenant, userId), request.IdempotencyKey); payroll.Add(approval);
        if (request.Approved)
        {
            foreach (var line in calculation.WorkerLines.SelectMany(x => x.EarningLines)) payroll.Add(PayrollEvidenceConsumption.Create(run.Id, calculation.Id, tenant.Id, farm.Id, line.EvidenceId, now));
            foreach (var deduction in calculation.WorkerLines.SelectMany(x => x.AdvanceDeductions)) payroll.Add(AdvanceRecovery.Create(run.Id, calculation.Id, deduction, now));
            PayrollAccess.Domain(() => period.Close(now, userId, PayrollAccess.OperationalPerson(tenant, userId), run.Id, period.Version), nameof(period.Version));
        }
        PayrollAudit.PayrollDecision(payroll, tenant, farm, user, run, approval, now); await payroll.SaveChangesAsync(cancellationToken); var response = await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken); await transaction.CommitAsync(cancellationToken); return response;
    }
}
