namespace Cane360.Application.MillRecords;

public sealed record StatementTicketMatchDto(Guid Id, Guid WeighbridgeTicketId,
    string TicketReference, string TicketDate, decimal TicketNetTonnes,
    decimal MatchedTonnes, decimal? MatchedAmountUsd, bool CompletesMatching,
    string? Reason, DateTimeOffset CreatedAt, bool Active, Guid? ReversedByMatchId);
