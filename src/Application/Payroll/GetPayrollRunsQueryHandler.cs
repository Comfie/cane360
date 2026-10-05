namespace Cane360.Application.Payroll;

public sealed class GetPayrollRunsQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user)
    : IRequestHandler<GetPayrollRunsQuery, IReadOnlyList<PayrollRunDto>>
{
    public async Task<IReadOnlyList<PayrollRunDto>> Handle(GetPayrollRunsQuery request,
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken, false);
        Dictionary<Guid, PayrollPeriod> periods =
            (await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(x => x.Id);
        return (await payroll.GetRunsAsync(tenant.Id, farm.Id, cancellationToken))
            .Select(run => PayrollRunMapper.MapSummary(run, periods[run.PayrollPeriodId], user)).ToArray();
    }
}
