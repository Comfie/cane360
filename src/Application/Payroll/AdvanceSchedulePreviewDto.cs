namespace Cane360.Application.Payroll;

public sealed record AdvanceSchedulePreviewDto(decimal AmountUsd, int InstallmentCount, IReadOnlyList<AdvanceInstallmentDto> Installments, decimal ScheduleTotalUsd);
