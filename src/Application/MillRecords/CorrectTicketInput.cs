namespace Cane360.Application.MillRecords;

public sealed record CorrectTicketInput(string Reason, string IdempotencyKey, TicketInput Replacement);
