namespace Cane360.Application.Payroll;

public static class AdvanceRecoveryAllocator
{
    public static IReadOnlyList<AdvanceRecoveryAllocation> Allocate(decimal grossAmountUsd, IEnumerable<AdvanceRecoveryCandidate> candidates)
    {
        if (grossAmountUsd < 0) throw new ArgumentOutOfRangeException(nameof(grossAmountUsd));
        var available = grossAmountUsd; var result = new List<AdvanceRecoveryAllocation>();
        foreach (var candidate in candidates.OrderBy(x => x.RecoveryYear).ThenBy(x => x.RecoveryMonth).ThenBy(x => x.AdvanceIssuedAt).ThenBy(x => x.WorkerAdvanceId).ThenBy(x => x.InstallmentSequence))
        {
            if (available <= 0) break;
            if (candidate.ScheduledAmountUsd <= 0 || candidate.OutstandingAmountUsd <= 0 || candidate.OutstandingAmountUsd > candidate.ScheduledAmountUsd) throw new InvalidOperationException("Advance recovery candidates require positive, reconciled scheduled and outstanding amounts.");
            var amount = Math.Min(available, candidate.OutstandingAmountUsd); result.Add(new AdvanceRecoveryAllocation(candidate, amount)); available -= amount;
        }
        return result;
    }
}
