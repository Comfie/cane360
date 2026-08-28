namespace Cane360.Application.Payroll;

public sealed class PreviewAdvanceScheduleQueryHandler(IFarmSetupRepository farms, IPayrollRepository payroll, IUser user) : IRequestHandler<PreviewAdvanceScheduleQuery, AdvanceSchedulePreviewDto>
{
    public async Task<AdvanceSchedulePreviewDto> Handle(PreviewAdvanceScheduleQuery request, CancellationToken cancellationToken)
    {
        var (tenant, farm, _) = await PayrollAccess.ContextAsync(farms, user, false, cancellationToken);
        var periods = await payroll.GetPeriodsAsync(tenant.Id, farm.Id, false, cancellationToken);
        var recovery = PayrollAccess.RequirePeriod(periods.SingleOrDefault(period => period.Id == request.RecoveryStartPayrollPeriodId), request.RecoveryStartPayrollPeriodId);
        var selected = AdvanceScheduleBuilder.SelectPeriods(periods, recovery, request.InstallmentCount);
        var installments = AdvanceScheduleBuilder.Preview(request.AmountUsd, selected);
        return new AdvanceSchedulePreviewDto(decimal.Round(request.AmountUsd, 2, MidpointRounding.AwayFromZero), request.InstallmentCount, installments, installments.Sum(item => item.AmountUsd));
    }
}
