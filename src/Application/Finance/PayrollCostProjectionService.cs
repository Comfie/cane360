using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Finance;

public sealed class PayrollCostProjectionService(IFinanceRepository finance, IPayrollRepository payroll,
    ILabourRepository labour, TimeProvider clock) : IPayrollCostProjectionService
{
    public async Task<PayrollCostReconciliationDto> ProjectAsync(Tenant tenant, Farm farm,
        PayrollRun run, PayrollCalculation calculation, IUser user,
        CancellationToken cancellationToken)
    {
        if (run.Status != PayrollRunStatus.Approved || run.SubmittedCalculationVersion != calculation.CalculationVersion ||
            calculation.PayrollRunId != run.Id)
            throw new ConflictException("Only the exact Grower-approved payroll calculation version can become crop cost.");

        int examined = 0;
        int added = 0;
        int preserved = 0;
        foreach (var earning in calculation.WorkerLines.SelectMany(x => x.EarningLines))
        {
            examined++;
            WorkRecord work = await labour.GetWorkRecordAsync(tenant.Id, farm.Id, earning.EvidenceId,
                false, cancellationToken) ?? throw new ConflictException(
                "Approved payroll earning evidence is unavailable for cost projection.");
            Field field = farm.Fields.SingleOrDefault(x => x.Id == earning.FieldId && x.Id == work.FieldId)
                ?? throw new ConflictException("Approved payroll earning field does not belong to this farm.");
            Guid[] activityIds = work.Activities.Select(x => x.ActivityId).Distinct().ToArray();
            CropCycle[] cycles = field.CropCycles.Where(cycle => cycle.Activities.Any(activity =>
                activityIds.Contains(activity.Id))).Distinct().ToArray();
            if (cycles.Length != 1)
                throw new ConflictException("Approved payroll earning evidence must resolve to exactly one crop cycle before it can become crop cost.");
            CropCycle cycle = cycles[0];
            if (await finance.HasPayrollCostPostingAsync(tenant.Id, farm.Id, earning.Id, cycle.Id,
                cancellationToken))
            { preserved++; continue; }

            Guid? activityId = activityIds.Length == 1 ? activityIds[0] : null;
            var posting = OperationalCostPosting.ForPayroll(tenant.Id, farm.Id, field.Id, activityId,
                cycle.Id, earning, $"payroll:{run.Id:N}:v{calculation.CalculationVersion}:earning:{earning.Id:N}");
            finance.Add(posting);
            Audit(finance, tenant, farm, user, posting, "PayrollCostProjected", clock.GetUtcNow(),
                $"Approved payroll earning projected as labour cost from calculation version {calculation.CalculationVersion}.");
            added++;
        }
        await finance.SaveChangesAsync(cancellationToken);
        return new(examined, added, preserved);
    }

    public async Task<PayrollCostReconciliationDto> ReconcileAsync(Tenant tenant, Farm farm,
        IUser user, CancellationToken cancellationToken)
    {
        IReadOnlyList<PayrollRun> approved = (await payroll.GetRunsAsync(tenant.Id, farm.Id,
            cancellationToken)).Where(x => x.Status == PayrollRunStatus.Approved &&
            x.SubmittedCalculationVersion.HasValue).ToArray();
        int examined = 0;
        int added = 0;
        int preserved = 0;
        foreach (PayrollRun run in approved)
        {
            PayrollCalculation calculation = await payroll.GetCalculationAsync(tenant.Id, farm.Id,
                run.Id, run.SubmittedCalculationVersion!.Value, cancellationToken)
                ?? throw new ConflictException("An approved payroll run is missing its exact calculation version.");
            PayrollCostReconciliationDto result = await ProjectAsync(tenant, farm, run, calculation,
                user, cancellationToken);
            examined += result.ApprovedEarningSourcesExamined;
            added += result.PostingsAdded;
            preserved += result.ExistingPostingsPreserved;
        }
        return new(examined, added, preserved);
    }

    private static void Audit(IFinanceRepository repository, Tenant tenant, Farm farm, IUser user,
        OperationalCostPosting posting, string action, DateTimeOffset at, string summary)
    {
        string userId = user.Id ?? throw new UnauthorizedAccessException();
        TenantMembership membership = tenant.Memberships.Single(x => x.UserId == userId);
        var audit = AuditEvent.Create(tenant.Id, farm.Id, nameof(OperationalCostPosting), posting.Id,
            action, userId, membership.SecurityRole, membership.PersonId, at,
            user.CorrelationId ?? Guid.NewGuid().ToString("N"), null, summary);
        repository.Add(audit);
        repository.Add(FinanceAuditEventLink.ForCostPosting(audit.Id, tenant.Id, farm.Id, posting.Id));
    }
}
