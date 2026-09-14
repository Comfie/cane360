namespace Cane360.Application.MillRecords;

public sealed record CandidateTicketDto(Guid Id, string TicketReference, string TicketDate,
    decimal NetTonnes, decimal AvailableTonnes, long Version, Guid? FieldId, string? FieldName,
    Guid? CropCycleId, string? CropCycleLabel, bool MatchedToAnotherStatement);
