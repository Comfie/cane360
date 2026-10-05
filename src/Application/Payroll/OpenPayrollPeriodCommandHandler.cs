namespace Cane360.Application.Payroll;

public sealed class OpenPayrollPeriodCommandHandler(
    IFarmSetupRepository farms,
    IPayrollRepository payroll,
    IUser user,
    TimeProvider clock) : IRequestHandler<OpenPayrollPeriodCommand, PayrollPeriodDto>
{
    public async Task<PayrollPeriodDto> Handle(OpenPayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, string userId) =
            await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        PayrollPeriod period = PayrollAccess.RequirePeriod(
            await payroll.GetPeriodAsync(tenant.Id, farm.Id, request.PayrollPeriodId, true, cancellationToken),
            request.PayrollPeriodId);
        PayrollAccess.Domain(
            () => period.Open(clock.GetUtcNow(), userId, PayrollAccess.OperationalPerson(tenant, userId),
                request.ExpectedVersion), nameof(request.ExpectedVersion));
        PayrollAudit.Period(payroll, tenant, farm, user, period, "PayrollPeriodOpened", clock.GetUtcNow(), null,
            "Payroll readiness period opened; evidence remains unconsumed.");
        await payroll.SaveChangesAsync(cancellationToken);
        return PayrollAccess.Period(period);
    }
}
