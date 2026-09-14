namespace Cane360.Application.MillRecords;

public sealed record CorrectStatementInput(string Reason, string IdempotencyKey,
    StatementInput Replacement);
