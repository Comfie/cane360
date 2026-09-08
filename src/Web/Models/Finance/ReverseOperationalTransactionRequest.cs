namespace Cane360.Web.Models.Finance;

public sealed record ReverseOperationalTransactionRequest(string Reason, string IdempotencyKey);
