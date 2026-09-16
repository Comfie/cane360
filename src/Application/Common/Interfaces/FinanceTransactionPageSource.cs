using Cane360.Application.Finance;

namespace Cane360.Application.Common.Interfaces;

public sealed record FinanceTransactionPageSource(IReadOnlyList<OperationalTransactionDto> Transactions,
    int TotalCount, decimal PostedExpenseUsd, decimal PostedIncomeUsd, int DraftCount);
