namespace Cane360.Application.Finance;

public sealed record FinanceTransactionFilter(DateOnly? From, DateOnly? To, string? Type,
    string? Category, string? Status, string? Search);
