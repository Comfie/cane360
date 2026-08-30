namespace Cane360.Application.Payroll;

public sealed class GetPayrollCalculationQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user) : IRequestHandler<GetPayrollCalculationQuery, PayrollCalculationDto>
{
    public async Task<PayrollCalculationDto> Handle(GetPayrollCalculationQuery request, CancellationToken cancellationToken)
    { var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); _ = PayrollAccess.RequireRun(await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, false, cancellationToken), request.PayrollRunId); var calculation = await payroll.GetCalculationAsync(tenant.Id, farm.Id, request.PayrollRunId, request.CalculationVersion, cancellationToken) ?? throw new NotFoundException(request.CalculationVersion.ToString(), "Payroll calculation"); return PayrollRunMapper.Map(calculation); }
}
