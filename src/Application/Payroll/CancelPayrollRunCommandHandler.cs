namespace Cane360.Application.Payroll;

public sealed class CancelPayrollRunCommandHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<CancelPayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(CancelPayrollRunCommand request, CancellationToken cancellationToken)
    { var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); PayrollAccess.RequireFarmManager(tenant, userId); var run = PayrollAccess.RequireRun(await payroll.GetRunAsync(tenant.Id, farm.Id, request.PayrollRunId, true, cancellationToken), request.PayrollRunId); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, run.PayrollPeriodId, false, cancellationToken), run.PayrollPeriodId); var now = clock.GetUtcNow(); PayrollAccess.Domain(() => run.Cancel(now, request.Reason, request.ExpectedVersion), nameof(request.ExpectedVersion)); PayrollAudit.Run(payroll, tenant, farm, user, run, "PayrollRunCancelled", now, request.Reason, "FarmManager cancelled an eligible payroll run; no evidence was consumed."); await payroll.SaveChangesAsync(cancellationToken); return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken); }
}
