namespace Cane360.Application.Payroll;

public sealed class CalculatePayrollRunCommandHandler(
    IFarmSetupRepository farms,
    ILabourRepository labour,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock) : IRequestHandler<CalculatePayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(CalculatePayrollRunCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) =
            await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        PayrollRun run = PayrollAccess.RequireRun(
            await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, true, cancellationToken),
            request.PayrollRunId);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, false, cancellationToken),
            run.PayrollPeriodId);
        int version = 0;
        PayrollAccess.Domain(() => version = run.RecordCalculation(request.ExpectedVersion),
            nameof(request.ExpectedVersion));
        DateTimeOffset now = clock.GetUtcNow();
        PayrollCalculation calculation = await PayrollCalculationBuilder.BuildAsync(farms, labour, payroll, tenant,
            farm, period, run, version, now, userId, PayrollAccess.OperationalPerson(tenant, userId),
            cancellationToken);
        payroll.Add(calculation);
        PayrollAudit.Calculation(payroll, tenant, farm, user, run, calculation, now);
        await payroll.SaveChangesAsync(cancellationToken);
        return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken);
    }
}
