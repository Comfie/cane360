namespace Cane360.Application.Payroll;

public sealed record PayrollPaymentReversalDto(Guid Id, decimal AmountUsd, string Reason,
    string ReversedByUserId, Guid? ReversedByPersonId, DateTimeOffset ReversedAt);
