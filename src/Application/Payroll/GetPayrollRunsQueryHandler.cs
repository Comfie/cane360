namespace Cane360.Application.Payroll;

public sealed class GetPayrollRunsQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user) : IRequestHandler<GetPayrollRunsQuery, IReadOnlyList<PayrollRunDto>>
{
    public async Task<IReadOnlyList<PayrollRunDto>> Handle(GetPayrollRunsQuery request, CancellationToken cancellationToken)
    { var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); var periods = (await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(x => x.Id); var result = new List<PayrollRunDto>(); foreach (var run in await payroll.GetRunsAsync(tenant.Id, farm.Id, cancellationToken)) result.Add(await PayrollRunMapper.MapAsync(payroll, run, periods[run.PayrollPeriodId], user, cancellationToken)); return result; }
}
