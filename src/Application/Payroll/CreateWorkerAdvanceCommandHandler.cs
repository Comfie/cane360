using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class CreateWorkerAdvanceCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<CreateWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(CreateWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken) ?? throw new NotFoundException(request.WorkerId.ToString(), "Worker");
        if (worker.Status != RecordStatus.Active) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.WorkerId), "Archived workers cannot receive new advances.")]);
        var periods = await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken); var recovery = PayrollAccess.RequirePeriod(periods.SingleOrDefault(x => x.Id == request.RecoveryStartPayrollPeriodId), request.RecoveryStartPayrollPeriodId); var count = request.InstallmentCount ?? 3; WorkerAdvance? advance = null;
        PayrollAccess.Domain(() => advance = WorkerAdvance.Create(tenant.Id, farm.Id, worker.Id, request.AmountUsd, request.Reason, request.RequestedEventDate, recovery.Id, count, clock.GetUtcNow(), userId, PayrollAccess.OperationalPerson(tenant, userId)), nameof(request.AmountUsd));
        if (request.InstallmentPeriodIds.Count == 0) request = request with { InstallmentPeriodIds = AdvanceScheduleBuilder.SelectPeriods(periods, recovery, count).Select(period => period.Id).ToArray() };
        var selected = request.InstallmentPeriodIds.Select(id => periods.SingleOrDefault(period => period.Id == id)).ToArray();
        if (request.InstallmentPeriodIds.Count != count || request.InstallmentPeriodIds.Distinct().Count() != count || selected.Any(period => period is null || period.Status == PayrollPeriodStatus.Cancelled || period.StartDate < recovery.StartDate) || !selected.Select(period => period!.StartDate).SequenceEqual(selected.Select(period => period!.StartDate).Order())) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.InstallmentPeriodIds), "Installments must reference distinct, ordered, non-cancelled periods on or after the recovery-start period.")]);
        PayrollAccess.Domain(() => advance!.SetSchedule(request.InstallmentPeriodIds, advance!.Version), nameof(request.InstallmentPeriodIds)); payroll.Add(advance!); PayrollAudit.Advance(payroll, tenant, farm, user, advance!, "AdvanceDraftCreated", clock.GetUtcNow(), null, "Worker advance draft created with an exact planned recovery schedule."); await payroll.SaveChangesAsync(cancellationToken); return await PayrollAccess.AdvanceAsync(payroll, advance!, new Dictionary<Guid, string> { [worker.Id] = farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName }, cancellationToken);
    }
}
