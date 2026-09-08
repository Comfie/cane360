namespace Cane360.Application.Finance;

public sealed record ReverseOperationalTransactionInput(string Reason, string IdempotencyKey);
