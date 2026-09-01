namespace Cane360.Application.Payroll;

public sealed record CashPaymentRegisterDto(string FarmName, string PayrollPeriod,
    Guid PayrollRunId, Guid PayrollCalculationId, int CalculationVersion,
    IReadOnlyList<CashPaymentRegisterRowDto> Workers, decimal TotalApprovedNetUsd,
    decimal TotalActiveCashPaidUsd, decimal TotalOutstandingUsd, DateTimeOffset GeneratedAt,
    string DocumentReference);
