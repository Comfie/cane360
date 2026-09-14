namespace Cane360.Application.MillRecords;

public sealed record StatementInput(Guid MillId, string StatementReference, DateOnly PeriodStart,
    DateOnly PeriodEnd, decimal TotalTonnes, decimal TotalAmountUsd, string? Notes,
    long ExpectedVersion = 0);
