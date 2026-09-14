namespace Cane360.Web.Models.MillRecords;

public sealed record CorrectStatementRequest(string Reason, string IdempotencyKey,
    StatementRequest Replacement);
