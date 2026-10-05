namespace Cane360.Application.Payroll;

public sealed class GetPayrollRunQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user)
    : IRequestHandler<GetPayrollRunQuery, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(GetPayrollRunQuery request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken, false);
        PayrollRun run = PayrollAccess.RequireRun(
            await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, false, cancellationToken),
            request.PayrollRunId);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, false, cancellationToken),
            run.PayrollPeriodId);
        return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken);
    }
}
