namespace Cane360.Web.Models.Finance;

public sealed record CreateOperationalTransactionRequest(string Type, string Category,
    string EventDate, string PayeeOrPayer, decimal AmountUsd, string? SourceReference, string? Notes);
