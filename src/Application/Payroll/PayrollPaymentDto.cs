namespace Cane360.Application.Payroll;

public sealed record PayrollPaymentDto(Guid Id, Guid PayrollRunId, Guid PayrollCalculationId,
    int CalculationVersion, Guid PayrollWorkerLineId, Guid WorkerProfileId, string Method,
    decimal AmountUsd, DateOnly PaymentDate, string ExternalStatus, string? Provider,
    string? MaskedRecipientNumber, string? TransactionReference, string RecordedByUserId,
    Guid? RecordedByPersonId, DateTimeOffset CreatedAt, decimal ReversedAmountUsd,
    decimal ActiveAmountUsd, PaymentAcknowledgementDto? Acknowledgement,
    IReadOnlyList<PayrollPaymentReversalDto> Reversals);
