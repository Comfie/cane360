namespace Cane360.Application.Payroll;

public sealed record OperationalPayslipDto(string DocumentStatement, string FarmName,
    string PayrollPeriod, Guid PayrollRunId, Guid PayrollCalculationId, int CalculationVersion,
    Guid PayrollWorkerLineId, string WorkerName, string MaskedWorkerIdentifier,
    IReadOnlyList<PayrollEarningLineDto> Earnings, decimal GrossAmountUsd,
    decimal DeductionAmountUsd, decimal AdvanceRecoveryUsd, decimal NetAmountUsd,
    decimal PaidAmountUsd, decimal OutstandingAmountUsd, string SettlementStatus,
    DateTimeOffset GeneratedAt, string DocumentReference);
