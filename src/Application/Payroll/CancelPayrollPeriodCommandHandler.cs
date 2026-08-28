using Cane360.Domain.Auditing;

namespace Cane360.Application.Payroll;

public sealed class CancelPayrollPeriodCommandHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user, TimeProvider clock) : IRequestHandler<CancelPayrollPeriodCommand, PayrollPeriodDto>
{
    public async Task<PayrollPeriodDto> Handle(CancelPayrollPeriodCommand request, CancellationToken cancellationToken)
    { var (tenant, farm, userId) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken); var period = PayrollAccess.RequirePeriod(await payroll.GetPeriodAsync(tenant.Id, farm.Id, request.PayrollPeriodId, true, cancellationToken), request.PayrollPeriodId); if (await payroll.HasApprovedOrIssuedInstallmentAsync(tenant.Id, farm.Id, period.Id, cancellationToken)) throw new ConflictException("A payroll period referenced by an approved or issued advance installment cannot be cancelled."); PayrollAccess.Domain(() => period.Cancel(clock.GetUtcNow(), userId, PayrollAccess.OperationalPerson(tenant, userId), request.Reason, request.ExpectedVersion), nameof(request.ExpectedVersion)); PayrollAudit.Period(payroll, tenant, farm, user, period, "PayrollPeriodCancelled", clock.GetUtcNow(), request.Reason, "Draft payroll period cancelled before payroll processing."); await payroll.SaveChangesAsync(cancellationToken); return PayrollAccess.Period(period); }
}
