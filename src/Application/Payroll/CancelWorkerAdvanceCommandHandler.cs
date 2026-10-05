using Cane360.Domain.Labour;

namespace Cane360.Application.Payroll;

public sealed class CancelWorkerAdvanceCommandHandler(
    IFarmSetupRepository farms,
    ILabourRepository labour,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock) : IRequestHandler<CancelWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(CancelWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) =
            await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        WorkerAdvance advance = PayrollAccess.RequireAdvance(
            await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, true, cancellationToken),
            request.AdvanceId);
        PayrollAccess.Domain(() => advance.Cancel(request.ExpectedVersion), nameof(request.ExpectedVersion));
        PayrollAudit.Advance(payroll, tenant, farm, user, advance, "AdvanceCancelled", clock.GetUtcNow(),
            request.Reason, "Draft or rejected advance cancelled without payroll effect.");
        await payroll.SaveChangesAsync(cancellationToken);
        WorkerProfile? worker =
            await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken);
        return await PayrollAccess.AdvanceAsync(payroll, advance,
            new Dictionary<Guid, string>
            {
                [advance.WorkerProfileId] = worker is null
                    ? "Worker"
                    : farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName
            }, cancellationToken);
    }
}
