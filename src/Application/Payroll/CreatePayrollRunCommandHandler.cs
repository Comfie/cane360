namespace Cane360.Application.Payroll;

public sealed class CreatePayrollRunCommandHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<CreatePayrollRunCommand, PayrollRunDto>
{
    public async Task<PayrollRunDto> Handle(CreatePayrollRunCommand request, CancellationToken cancellationToken)
    { var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); PayrollAccess.RequireFarmManager(tenant, userId); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, request.PayrollPeriodId, false, cancellationToken), request.PayrollPeriodId); if (period.Status != PayrollPeriodStatus.Open) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(request.PayrollPeriodId), "Only an open payroll period may receive a run.")]); var now = clock.GetUtcNow(); var run = PayrollRun.Create(tenant.Id, farm.Id, period.Id, now, userId, PayrollAccess.OperationalPerson(tenant, userId)); payroll.Add(run); PayrollAudit.Run(payroll, tenant, farm, user, run, "PayrollRunCreated", now, null, "FarmManager created a payroll run for an open monthly period."); await payroll.SaveChangesAsync(cancellationToken); return await PayrollRunMapper.MapAsync(payroll, run, period, user, cancellationToken); }
}
