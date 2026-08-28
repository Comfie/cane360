using Cane360.Domain.Payroll;

namespace Cane360.Application.Payroll;

internal static class AdvanceScheduleBuilder
{
    public static IReadOnlyList<PayrollPeriod> SelectPeriods(IReadOnlyList<PayrollPeriod> periods, PayrollPeriod recovery, int count)
    {
        if (count <= 0) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(count), "Installment count must be positive.")]);
        var selected = periods.Where(period => period.Status != PayrollPeriodStatus.Cancelled && period.StartDate >= recovery.StartDate).OrderBy(period => period.StartDate).Take(count).ToArray();
        if (selected.Length != count) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(count), "Create enough non-cancelled monthly payroll periods to cover the recovery schedule.")]);
        return selected;
    }

    public static IReadOnlyList<AdvanceInstallmentDto> Preview(decimal amountUsd, IReadOnlyList<PayrollPeriod> periods)
    {
        if (amountUsd <= 0) throw new Cane360.Application.Common.Exceptions.ValidationException([new FluentValidation.Results.ValidationFailure(nameof(amountUsd), "Advance amount must be positive.")]);
        var amount = decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero);
        var baseAmount = decimal.Floor((amount / periods.Count) * 100m) / 100m;
        return periods.Select((period, index) => new AdvanceInstallmentDto(index + 1, period.Id, index == periods.Count - 1 ? amount - (baseAmount * (periods.Count - 1)) : baseAmount)).ToArray();
    }
}
