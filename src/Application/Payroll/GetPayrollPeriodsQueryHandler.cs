namespace Cane360.Application.Payroll;

public sealed class GetPayrollPeriodsQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user) : IRequestHandler<GetPayrollPeriodsQuery, IReadOnlyList<PayrollPeriodDto>>
{
    public async Task<IReadOnlyList<PayrollPeriodDto>> Handle(GetPayrollPeriodsQuery request, CancellationToken cancellationToken) { var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); return (await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken)).Select(PayrollAccess.Period).ToArray(); }
}
