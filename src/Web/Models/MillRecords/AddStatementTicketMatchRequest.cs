namespace Cane360.Web.Models.MillRecords;

public sealed record AddStatementTicketMatchRequest(Guid WeighbridgeTicketId,
    decimal? MatchedTonnes, decimal? MatchedAmountUsd, bool CompletesMatching,
    string? Reason, string IdempotencyKey, long ExpectedStatementVersion,
    long ExpectedTicketVersion);
