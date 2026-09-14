namespace Cane360.Web.Models.MillRecords;

public sealed record TicketRequest(Guid MillId, string TicketReference, string TicketDate,
    decimal GrossTonnes, decimal? TareTonnes, decimal NetTonnes, Guid? FieldId,
    Guid? CropCycleId, string? SourceReference, string? Notes, long ExpectedVersion);
