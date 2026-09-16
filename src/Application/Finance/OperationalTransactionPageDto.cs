namespace Cane360.Application.Finance;

public sealed record OperationalTransactionPageDto(IReadOnlyList<OperationalTransactionDto> Items,
    int Page, int PageSize, int TotalCount, decimal PostedExpenseUsd,
    decimal PostedIncomeUsd, int DraftCount);
