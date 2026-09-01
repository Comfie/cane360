namespace Cane360.Application.Payroll;

public sealed record WorkerSettlementDto(Guid PayrollWorkerLineId, Guid WorkerProfileId,
    string WorkerName, decimal GrossAmountUsd, decimal DeductionAmountUsd, decimal ApprovedNetUsd,
    decimal ValidPaidAmountUsd, decimal ReversedAmountUsd, decimal OutstandingAmountUsd,
    int PaymentCount, string PaymentMethodSummary, bool AcknowledgementComplete,
    string SettlementStatus, IReadOnlyList<PayrollPaymentDto> Payments);
