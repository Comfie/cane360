using Cane360.Domain.MillRecords;

namespace Cane360.Application.MillRecords;

public static class MillReconciliationMath
{
    public static ReconciliationStatus Status(decimal statementTonnes, decimal matchedTonnes,
        int activeMatchCount, bool matchingCompleted) => activeMatchCount == 0
        ? ReconciliationStatus.Unmatched
        : !matchingCompleted
            ? ReconciliationStatus.PartiallyMatched
            : statementTonnes == matchedTonnes
                ? ReconciliationStatus.Matched
                : ReconciliationStatus.Variance;

    public static AmountReconciliationStatus AmountStatus(decimal statementAmountUsd,
        decimal? matchedAmountUsd, int activeMatchCount, int populatedAmountCount) =>
        populatedAmountCount == 0 ? AmountReconciliationStatus.NotAvailable :
        populatedAmountCount < activeMatchCount ? AmountReconciliationStatus.Incomplete :
        statementAmountUsd == matchedAmountUsd ? AmountReconciliationStatus.Matched :
        AmountReconciliationStatus.Variance;
}
