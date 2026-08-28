namespace Cane360.Application.Payroll;

public sealed class GetWorkerAdvancesQueryHandler(IFarmSetupRepository farms, ILabourRepository labour, IPayrollRepository payroll, IUser user) : IRequestHandler<GetWorkerAdvancesQuery, IReadOnlyList<WorkerAdvanceDto>>
{
    public async Task<IReadOnlyList<WorkerAdvanceDto>> Handle(GetWorkerAdvancesQuery request, CancellationToken cancellationToken)
    {
        var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var workers = (await labour.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(x => x.Id, x => farm.Persons.Single(p => p.Id == x.PersonId).DisplayName);
        var result = new List<WorkerAdvanceDto>();
        foreach (var advance in await payroll.GetAdvancesAsync(tenant.Id, farm.Id, false, cancellationToken)) result.Add(await PayrollAccess.AdvanceAsync(payroll, advance, workers, cancellationToken));
        return result;
    }
}
