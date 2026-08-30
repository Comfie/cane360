namespace Cane360.Application.Payroll;

public sealed class GetPayrollRunQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user) : IRequestHandler<GetPayrollRunQuery, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(GetPayrollRunQuery request, CancellationToken cancellationToken)
    { var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); var run = PayrollAccess.RequireRun(await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, false, cancellationToken), request.PayrollRunId); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, false, cancellationToken), run.PayrollPeriodId); return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken); }
}
