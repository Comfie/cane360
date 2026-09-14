namespace Cane360.Application.MillRecords;

public sealed record TicketFilter(DateOnly? From, DateOnly? To, Guid? MillId, Guid? FieldId,
    Guid? CropCycleId, string? Status, string? MatchStatus, string? Search);
