namespace Cane360.Application.Payroll;

public sealed record CashPaymentRegisterRowDto(Guid PayrollWorkerLineId, string WorkerName,
    string MaskedWorkerIdentifier, decimal ApprovedNetUsd, decimal CashAmountPaidUsd,
    DateOnly? LastCashPaymentDate, string AcknowledgementState, decimal OutstandingAmountUsd);
