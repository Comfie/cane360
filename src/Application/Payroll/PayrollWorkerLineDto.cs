namespace Cane360.Application.Payroll;

public sealed record PayrollWorkerLineDto(Guid Id, Guid WorkerId, string WorkerName, decimal GrossAmountUsd, decimal DeductionAmountUsd, decimal NetAmountUsd, IReadOnlyList<PayrollEarningLineDto> Earnings, IReadOnlyList<PayrollAdvanceDeductionDto> AdvanceDeductions);
