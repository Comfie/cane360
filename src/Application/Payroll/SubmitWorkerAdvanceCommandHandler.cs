using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class SubmitWorkerAdvanceCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<SubmitWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(SubmitWorkerAdvanceCommand request, CancellationToken cancellationToken) { var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); if (PayrollAccess.Role(tenant, userId) != TenantSecurityRoles.FarmManager) throw new ForbiddenAccessException(); var advance = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, true, cancellationToken), request.AdvanceId); PayrollAccess.Domain(() => advance.Submit(request.ExpectedVersion), nameof(request.ExpectedVersion)); PayrollAudit.Advance(payroll, tenant, farm, user, advance, "AdvanceSubmitted", clock.GetUtcNow(), null, "Exact advance version submitted for Grower decision."); await payroll.SaveChangesAsync(cancellationToken); var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken); return await PayrollAccess.AdvanceAsync(payroll, advance, new Dictionary<Guid, string> { [advance.WorkerProfileId] = worker is null ? "Worker" : farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName }, cancellationToken); }
}
