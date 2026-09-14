namespace Cane360.Application.MillRecords;

public sealed record TicketInput(Guid MillId, string TicketReference, DateOnly TicketDate,
    decimal GrossTonnes, decimal? TareTonnes, decimal NetTonnes, Guid? FieldId,
    Guid? CropCycleId, string? SourceReference, string? Notes, long ExpectedVersion = 0);
