namespace Cane360.Web.Models.MillRecords;

public sealed record ReverseStatementTicketMatchRequest(string Reason, string IdempotencyKey);
