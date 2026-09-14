namespace Cane360.Application.MillRecords;

public sealed record StatementFilter(DateOnly? From, DateOnly? To, Guid? MillId,
    string? MatchStatus, string? Search);
