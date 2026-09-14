namespace Cane360.Application.MillRecords;

public sealed record AddMatchInput(Guid WeighbridgeTicketId, decimal? MatchedTonnes,
    decimal? MatchedAmountUsd, bool CompletesMatching, string? Reason,
    string IdempotencyKey, long ExpectedStatementVersion, long ExpectedTicketVersion);
