using Cane360.Domain.Labour;
using FluentValidation.Results;
using ValidationException = Cane360.Application.Common.Exceptions.ValidationException;

namespace Cane360.Application.Payroll;

public sealed class CreateWorkerAdvanceCommandHandler(
    IFarmSetupRepository farms,
    ILabourRepository labour,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock) : IRequestHandler<CreateWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(CreateWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) =
            await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        WorkerProfile worker =
            await labour.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken) ??
            throw new NotFoundException(request.WorkerId.ToString(), "Worker");
        if (worker.Status != RecordStatus.Active)
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.WorkerId), "Archived workers cannot receive new advances.")
            ]);
        }

        IReadOnlyList<PayrollPeriod> periods =
            await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken);
        PayrollPeriod recovery = PayrollAccess.RequirePeriod(
            periods.SingleOrDefault(x => x.Id == request.RecoveryStartPayrollPeriodId),
            request.RecoveryStartPayrollPeriodId);
        int count = request.InstallmentCount ?? 3;
        WorkerAdvance? advance = null;
        PayrollAccess.Domain(
            () => advance = WorkerAdvance.Create(tenant.Id, farm.Id, worker.Id, request.AmountUsd, request.Reason,
                request.RequestedEventDate, recovery.Id, count, clock.GetUtcNow(), userId,
                PayrollAccess.OperationalPerson(tenant, userId)), nameof(request.AmountUsd));
        if (request.InstallmentPeriodIds.Count == 0)
        {
            request = request with
            {
                InstallmentPeriodIds = AdvanceScheduleBuilder.SelectPeriods(periods, recovery, count)
                    .Select(period => period.Id).ToArray()
            };
        }

        PayrollPeriod?[] selected = request.InstallmentPeriodIds
            .Select(id => periods.SingleOrDefault(period => period.Id == id)).ToArray();
        if (request.InstallmentPeriodIds.Count != count || request.InstallmentPeriodIds.Distinct().Count() != count ||
            selected.Any(period => period is null || period.Status == PayrollPeriodStatus.Cancelled ||
                                   period.StartDate < recovery.StartDate) || !selected
                .Select(period => period!.StartDate)
                .SequenceEqual(selected.Select(period => period!.StartDate).Order()))
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.InstallmentPeriodIds),
                    "Installments must reference distinct, ordered, non-cancelled periods on or after the recovery-start period.")
            ]);
        }

        PayrollAccess.Domain(() => advance!.SetSchedule(request.InstallmentPeriodIds, advance!.Version),
            nameof(request.InstallmentPeriodIds));
        payroll.Add(advance!);
        PayrollAudit.Advance(payroll, tenant, farm, user, advance!, "AdvanceDraftCreated", clock.GetUtcNow(), null,
            "Worker advance draft created with an exact planned recovery schedule.");
        await payroll.SaveChangesAsync(cancellationToken);
        return await PayrollAccess.AdvanceAsync(payroll, advance!,
            new Dictionary<Guid, string>
            {
                [worker.Id] = farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName
            }, cancellationToken);
    }
}
