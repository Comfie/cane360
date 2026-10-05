namespace Cane360.Application.Payroll;

public sealed class PreviewAdvanceScheduleQueryHandler(
    IFarmSetupRepository farms,
    IPayrollRepository payroll,
    IUser user) : IRequestHandler<PreviewAdvanceScheduleQuery, AdvanceSchedulePreviewDto>
{
    public async Task<AdvanceSchedulePreviewDto> Handle(PreviewAdvanceScheduleQuery request,
        CancellationToken cancellationToken)
    {
        (Tenant tenant, Farm farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        IReadOnlyList<PayrollPeriod> periods =
            await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken);
        PayrollPeriod recovery = PayrollAccess.RequirePeriod(
            periods.SingleOrDefault(period => period.Id == request.RecoveryStartPayrollPeriodId),
            request.RecoveryStartPayrollPeriodId);
        IReadOnlyList<PayrollPeriod> selected =
            AdvanceScheduleBuilder.SelectPeriods(periods, recovery, request.InstallmentCount);
        IReadOnlyList<AdvanceInstallmentDto> installments = AdvanceScheduleBuilder.Preview(request.AmountUsd, selected);
        return new AdvanceSchedulePreviewDto(decimal.Round(request.AmountUsd, 2, MidpointRounding.AwayFromZero),
            request.InstallmentCount, installments, installments.Sum(item => item.AmountUsd));
    }
}
