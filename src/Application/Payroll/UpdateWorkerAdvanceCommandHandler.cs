using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class UpdateWorkerAdvanceCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<UpdateWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(UpdateWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var advance = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, true, cancellationToken), request.AdvanceId);
        var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken) ?? throw new NotFoundException(advance.WorkerProfileId.ToString(), "Worker");
        if (worker.Status != RecordStatus.Active) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(advance.WorkerProfileId), "Archived workers cannot receive new advances.")]);
        var periods = await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken);
        var recovery = PayrollAccess.RequirePeriod(periods.SingleOrDefault(period => period.Id == request.RecoveryStartPayrollPeriodId), request.RecoveryStartPayrollPeriodId);
        var schedulePeriods = AdvanceScheduleBuilder.SelectPeriods(periods, recovery, request.InstallmentCount);
        var replacedInstallments = advance.Installments.ToArray();
        payroll.RemoveDraftInstallments(replacedInstallments);
        PayrollAccess.Domain(() => advance.Edit(request.AmountUsd, request.Reason, request.RequestedEventDate, recovery.Id, request.InstallmentCount, request.ExpectedVersion), nameof(request.ExpectedVersion));
        PayrollAccess.Domain(() => advance.SetSchedule(schedulePeriods.Select(period => period.Id).ToArray(), advance.Version), nameof(request.InstallmentCount));
        PayrollAudit.Advance(payroll, tenant, farm, user, advance, "AdvanceDraftRevised", clock.GetUtcNow(), null, "Draft advance and planned installments revised; any prior rejection remains immutable.");
        await payroll.SaveChangesAsync(cancellationToken);
        return await PayrollAccess.AdvanceAsync(payroll, advance, new Dictionary<Guid, string> { [worker.Id] = farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName }, cancellationToken);
    }
}
