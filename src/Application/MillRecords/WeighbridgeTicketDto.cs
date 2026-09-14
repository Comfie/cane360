namespace Cane360.Application.MillRecords;

public sealed record WeighbridgeTicketDto(Guid Id, Guid MillId, string MillCode,
    string MillName, string TicketReference, string TicketDate, decimal GrossTonnes,
    decimal? TareTonnes, decimal NetTonnes, Guid? FieldId, string? FieldName,
    Guid? CropCycleId, string? CropCycleLabel, decimal? RecordedHarvestTonnes,
    string? SourceReference, string? Notes, string Status, long Version,
    DateTimeOffset CreatedAt, DateTimeOffset? RecordedAt, Guid? CorrectsTicketId,
    string? CorrectionReason, Guid? CorrectedByTicketId, bool IsCurrent,
    IReadOnlyList<EvidenceDocumentDto> Evidence, IReadOnlyList<Guid> MatchedStatementIds,
    bool MatchedToAnotherStatement);
