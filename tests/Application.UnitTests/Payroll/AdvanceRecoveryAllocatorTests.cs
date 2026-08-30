using Cane360.Application.Payroll;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Payroll;

public sealed class AdvanceRecoveryAllocatorTests
{
    [Test]
    public void OverdueInstallmentsAndEarlierIssuesRecoverFirstAcrossMultipleAdvances()
    {
        var earlierAdvance = Guid.Parse("00000000-0000-0000-0000-000000000001"); var laterAdvance = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var candidates = new[]
        {
            Candidate(laterAdvance, 2026, 8, new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero), 1, 20m),
            Candidate(earlierAdvance, 2026, 7, new DateTimeOffset(2026, 1, 3, 0, 0, 0, TimeSpan.Zero), 2, 30m),
            Candidate(earlierAdvance, 2026, 8, new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero), 1, 10m)
        };
        var result = AdvanceRecoveryAllocator.Allocate(100m, candidates);
        result.Select(x => x.Candidate.RecoveryMonth).ShouldBe([7, 8, 8]); result.Select(x => x.Candidate.WorkerAdvanceId).ShouldBe([earlierAdvance, earlierAdvance, laterAdvance]); result.Sum(x => x.AmountUsd).ShouldBe(60m);
    }

    [Test]
    public void InsufficientGrossCreatesPartialRecoveryAndNeverNegativeNet()
    {
        var result = AdvanceRecoveryAllocator.Allocate(25m, [Candidate(Guid.NewGuid(), 2026, 7, DateTimeOffset.UtcNow, 1, 20m), Candidate(Guid.NewGuid(), 2026, 8, DateTimeOffset.UtcNow, 1, 20m)]);
        result.Select(x => x.AmountUsd).ShouldBe([20m, 5m]); result.Sum(x => x.AmountUsd).ShouldBe(25m); (25m - result.Sum(x => x.AmountUsd)).ShouldBe(0m);
    }

    [Test]
    public void ZeroGrossCreatesNoRecoveryFact()
    {
        AdvanceRecoveryAllocator.Allocate(0m, [Candidate(Guid.NewGuid(), 2026, 8, DateTimeOffset.UtcNow, 1, 10m)]).ShouldBeEmpty();
    }

    private static AdvanceRecoveryCandidate Candidate(Guid advanceId, int year, int month, DateTimeOffset issuedAt, int sequence, decimal outstanding) => new(advanceId, Guid.NewGuid(), Guid.NewGuid(), year, month, issuedAt, sequence, outstanding, outstanding);
}
