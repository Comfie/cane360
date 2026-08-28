namespace Cane360.Application.Payroll;

public sealed record AdvanceInstallmentDto(int Sequence, Guid PayrollPeriodId, decimal AmountUsd);
