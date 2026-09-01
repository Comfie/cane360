namespace Cane360.Application.Payroll;

public sealed record RunSettlementDto(Guid PayrollRunId, Guid PayrollCalculationId,
    int CalculationVersion, string FarmName, string PayrollPeriod, decimal GrossAmountUsd,
    decimal DeductionAmountUsd, decimal NetAmountUsd, decimal PaidAmountUsd,
    decimal ReversedAmountUsd, decimal OutstandingAmountUsd, int WorkerCount,
    int WorkersSettled, int WorkersOutstanding, int AcknowledgementExceptions,
    string SettlementStatus, bool IsClosed, bool CanClose, IReadOnlyList<WorkerSettlementDto> Workers);
