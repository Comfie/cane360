namespace Cane360.Application.Payroll;

public sealed class GetWorkerAdvanceQueryHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user) : IRequestHandler<GetWorkerAdvanceQuery, WorkerAdvanceDto>
{
    public async Task<WorkerAdvanceDto> Handle(GetWorkerAdvanceQuery request, CancellationToken cancellationToken)
    {
        var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var advance = PayrollAccess.RequireAdvance(await payroll.GetAdvanceAsync(tenant.Id, farm.Id, request.AdvanceId, false, cancellationToken), request.AdvanceId);
        var worker = await labour.GetWorkerAsync(tenant.Id, farm.Id, advance.WorkerProfileId, false, cancellationToken);
        return await PayrollAccess.AdvanceAsync(payroll, advance, new Dictionary<Guid, string> { [advance.WorkerProfileId] = worker is null ? "Worker" : farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName }, cancellationToken);
    }
}
