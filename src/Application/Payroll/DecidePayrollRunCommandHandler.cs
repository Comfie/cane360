using System.Text.Json;

namespace Cane360.Application.Payroll;

public sealed class DecidePayrollRunCommandHandler(
    IFarmSetupRepository farms,
    ILabourRepository labour,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock,
    IPayrollCostProjectionService? costProjection = null) : IRequestHandler<DecidePayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(DecidePayrollRunCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) = await PayrollAccess.ContextAsync(farms, user, false,
            cancellationToken);
        await using IPayrollTransaction
            transaction = await payroll.BeginSerializableTransactionAsync(cancellationToken);
        PayrollApproval? existing =
            await payroll.GetPayrollApprovalByKeyAsync(tenant.Id, farm.Id, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.PayrollRunId != request.PayrollRunId || existing.RunVersion != request.ExpectedVersion ||
                existing.CalculationVersion != request.CalculationVersion || existing.Approved != request.Approved)
            {
                throw new ConflictException(
                    "This payroll decision idempotency key is bound to a different exact version or outcome.");
            }

            PayrollRun prior = PayrollAccess.RequireRun(
                await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, false, cancellationToken),
                request.PayrollRunId);
            PayrollPeriod priorPeriod = PayrollAccess.RequirePeriod(
                await payroll.GetPeriodAsync(tenant.Id, farm.Id, prior.PayrollPeriodId, false, cancellationToken),
                prior.PayrollPeriodId);
            PayrollRunDto result =
                await PayrollRunMapper.MapAsync(payroll, prior, priorPeriod, user, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        PayrollRun run = PayrollAccess.RequireRun(
            await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, true, cancellationToken),
            request.PayrollRunId);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, true, cancellationToken),
            run.PayrollPeriodId);
        PayrollCalculation calculation =
            await payroll.GetCalculationAsync(tenant.Id, farm.Id, run.Id, request.CalculationVersion,
                cancellationToken) ??
            throw new NotFoundException(request.CalculationVersion.ToString(), "Payroll calculation");
        if (run.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This payroll run changed after it was loaded. Refresh and try again.");
        }

        if (run.Status != PayrollRunStatus.PendingGrowerApproval ||
            run.SubmittedCalculationVersion != request.CalculationVersion)
        {
            throw new ConflictException(
                "The exact submitted payroll calculation version is no longer pending Grower approval.");
        }

        DateTimeOffset now = clock.GetUtcNow();
        if (request.Approved)
        {
            PayrollCalculation fresh = await PayrollCalculationBuilder.BuildAsync(farms, labour, payroll, tenant, farm,
                period, run, request.CalculationVersion, now, userId, PayrollAccess.OperationalPerson(tenant, userId),
                cancellationToken);
            string[] freshBlockers = JsonSerializer.Deserialize<string[]>(fresh.BlockerSnapshot) ?? [];
            List<string> staleCodes = freshBlockers.ToList();
            Dictionary<Guid, PayrollEarningLine> originalLines =
                calculation.WorkerLines.SelectMany(x => x.EarningLines).ToDictionary(x => x.EvidenceId);
            Dictionary<Guid, PayrollEarningLine> freshLines =
                fresh.WorkerLines.SelectMany(x => x.EarningLines).ToDictionary(x => x.EvidenceId);
            if (!originalLines.Keys.Order().SequenceEqual(freshLines.Keys.Order()))
            {
                staleCodes.Add(PayrollPreflightBlockerCodes.EvidenceChangedAfterCalculation);
            }

            foreach (KeyValuePair<Guid, PayrollEarningLine> pair in originalLines.Where(x =>
                         freshLines.ContainsKey(x.Key)))
            {
                PayrollEarningLine next = freshLines[pair.Key];
                if (pair.Value.SourceFingerprint != next.SourceFingerprint ||
                    pair.Value.AttendanceVersion != next.AttendanceVersion)
                {
                    staleCodes.Add(PayrollPreflightBlockerCodes.EvidenceChangedAfterCalculation);
                }

                if (pair.Value.SupervisorVerifiedAtSnapshot != next.SupervisorVerifiedAtSnapshot ||
                    pair.Value.ManagerConfirmedAtSnapshot != next.ManagerConfirmedAtSnapshot)
                {
                    staleCodes.Add(PayrollPreflightBlockerCodes.VerificationChanged);
                }

                if (pair.Value.RateSourceId != next.RateSourceId || pair.Value.RateVersion != next.RateVersion ||
                    pair.Value.RateAmountUsd != next.RateAmountUsd)
                {
                    staleCodes.Add(PayrollPreflightBlockerCodes.RateSnapshotChanged);
                }
            }

            string[] originalDeductions = calculation.WorkerLines.SelectMany(x => x.AdvanceDeductions).Select(x =>
                    $"{x.WorkerAdvanceId:N}:{x.AdvanceInstallmentId:N}:{x.OutstandingBeforeUsd}:{x.AmountUsd}").Order()
                .ToArray();
            string[] freshDeductions = fresh.WorkerLines.SelectMany(x => x.AdvanceDeductions).Select(x =>
                    $"{x.WorkerAdvanceId:N}:{x.AdvanceInstallmentId:N}:{x.OutstandingBeforeUsd}:{x.AmountUsd}").Order()
                .ToArray();
            if (!originalDeductions.SequenceEqual(freshDeductions))
            {
                staleCodes.Add(PayrollPreflightBlockerCodes.AdvanceChangedAfterCalculation);
            }

            if (fresh.SourceFingerprint != calculation.SourceFingerprint ||
                fresh.GrossAmountUsd != calculation.GrossAmountUsd ||
                fresh.DeductionAmountUsd != calculation.DeductionAmountUsd ||
                fresh.NetAmountUsd != calculation.NetAmountUsd || staleCodes.Count != 0)
            {
                staleCodes.Add(PayrollPreflightBlockerCodes.PayrollCalculationStale);
                throw new ConflictException(
                    $"PayrollCalculationStale: authoritative payroll sources changed after calculation. Recalculate and resubmit. Details: {string.Join(", ", staleCodes.Distinct())}");
            }
        }

        long subjectVersion = run.Version;
        PayrollAccess.Domain(
            () => run.Decide(request.Approved, request.CalculationVersion, now, request.Reason,
                request.ExpectedVersion), nameof(request.ExpectedVersion));
        PayrollApproval approval = PayrollApproval.Create(run.Id, calculation.Id, tenant.Id, farm.Id, subjectVersion,
            request.CalculationVersion, request.Approved, request.Reason, now, userId,
            PayrollAccess.OperationalPerson(tenant, userId), request.IdempotencyKey);
        payroll.Add(approval);
        if (request.Approved)
        {
            foreach (PayrollEarningLine line in calculation.WorkerLines.SelectMany(x => x.EarningLines))
            {
                payroll.Add(PayrollEvidenceConsumption.Create(run.Id, calculation.Id, tenant.Id, farm.Id,
                    line.EvidenceId, now));
            }

            foreach (PayrollAdvanceDeduction deduction in calculation.WorkerLines.SelectMany(x => x.AdvanceDeductions))
            {
                payroll.Add(AdvanceRecovery.Create(run.Id, calculation.Id, deduction, now));
            }

            PayrollAccess.Domain(
                () => period.Close(now, userId, PayrollAccess.OperationalPerson(tenant, userId), run.Id,
                    period.Version), nameof(period.Version));
        }

        PayrollAudit.PayrollDecision(payroll, tenant, farm, user, run, approval, now);
        await payroll.SaveChangesAsync(cancellationToken);
        if (request.Approved && costProjection is not null)
        {
            await costProjection.ProjectAsync(tenant, farm, run, calculation, user, cancellationToken);
        }

        PayrollRunDto response = await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return response;
    }
}
