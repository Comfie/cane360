using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class CreatePayrollPeriodCommandHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<CreatePayrollPeriodCommand, PayrollPeriodDto>
{
    public async Task<PayrollPeriodDto> Handle(CreatePayrollPeriodCommand request, CancellationToken cancellationToken)
    {
        var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); PayrollPeriod? period = null;
        PayrollAccess.Domain(() => period = PayrollPeriod.Create(tenant.Id, farm.Id, request.Year, request.Month, clock.GetUtcNow(), userId, PayrollAccess.OperationalPerson(tenant, userId)), nameof(request.Month));
        payroll.Add(period!); PayrollAudit.Period(payroll, tenant, farm, user, period!, "PayrollPeriodCreated", clock.GetUtcNow(), null, "Monthly payroll period created without calculation."); await payroll.SaveChangesAsync(cancellationToken); return PayrollAccess.Period(period!);
    }
}
