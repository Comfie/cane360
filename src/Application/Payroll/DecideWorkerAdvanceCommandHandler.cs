using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class DecideWorkerAdvanceCommandHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<DecideWorkerAdvanceCommand, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(DecideWorkerAdvanceCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        PayrollAccess.RequireGrower(tenant, userId);
        var existing = await payroll.GetApprovalByKeyAsync(tenant.Id, farm.Id, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (existing.WorkerAdvanceId != request.AdvanceId || existing.AdvanceVersion != request.ExpectedVersion || existing.Approved != request.Approved) throw new ConflictException("This approval idempotency key is already bound to a different decision.");
            var prior = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, existing.WorkerAdvanceId, false, cancellationToken), existing.WorkerAdvanceId);
            var pworker = await labour.GetWorkerAsync(tenant.Id, farm.Id, prior.WorkerProfileId, false, cancellationToken);
            return await PayrollAccess.AdvanceAsync(payroll, prior, new Dictionary<Guid, string> { [prior.WorkerProfileId] = pworker is null ? "Worker" : farm.Persons.Single(x => x.Id == pworker.PersonId).DisplayName }, cancellationToken);
        }
        var advance = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, true, cancellationToken), request.AdvanceId); var version = advance.Version;
        PayrollAccess.Domain(() => advance.Decide(request.Approved, request.ExpectedVersion), nameof(request.ExpectedVersion));
        var approval = AdvanceApproval.Create(advance.Id, tenant.Id, farm.Id, version, advance.RequestedAmountUsd, advance.Installments, request.Approved, userId, clock.GetUtcNow(), request.Reason, request.IdempotencyKey); payroll.Add(approval); PayrollAudit.Approval(payroll, tenant, farm, user, advance, approval, request.Approved ? "AdvanceApproved" : "AdvanceRejected", clock.GetUtcNow(), request.Reason, "Grower decided the exact submitted advance version and installment schedule; no money was issued."); await payroll.SaveChangesAsync(cancellationToken);
        var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken); return await PayrollAccess.AdvanceAsync(payroll, advance, new Dictionary<Guid, string> { [advance.WorkerProfileId] = worker is null ? "Worker" : farm.Persons.Single(x => x.Id == worker.PersonId).DisplayName }, cancellationToken);
    }
}
