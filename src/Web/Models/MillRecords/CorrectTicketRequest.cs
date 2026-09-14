namespace Cane360.Web.Models.MillRecords;

public sealed record CorrectTicketRequest(string Reason, string IdempotencyKey,
    TicketRequest Replacement);
