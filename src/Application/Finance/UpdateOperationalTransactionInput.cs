namespace Cane360.Application.Finance;

public sealed record UpdateOperationalTransactionInput(string Type, string Category, DateOnly EventDate,
    string PayeeOrPayer, decimal AmountUsd, string? SourceReference, string? Notes, long ExpectedVersion);
