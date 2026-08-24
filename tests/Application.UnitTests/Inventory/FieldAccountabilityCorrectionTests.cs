using Cane360.Domain.Inventory;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class FieldAccountabilityCorrectionTests
{
    [Test]
    public void GrowerDecisionIsBoundToTheRequestedVersionAndCannotBeReplayed()
    {
        var correction = FieldAccountabilityCorrection.ForLoss(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 7, "Correct the approved loss", "manager", "AUTOTEST-P5C-correction", DateTimeOffset.UtcNow);

        Should.Throw<InvalidOperationException>(() => correction.Decide(ApprovalOutcome.Approved, DateTimeOffset.UtcNow, 2));

        correction.Decide(ApprovalOutcome.Approved, DateTimeOffset.UtcNow, 1);
        correction.Status.ShouldBe(FieldAccountabilityCorrectionStatus.Approved);
        correction.Version.ShouldBe(2);

        Should.Throw<InvalidOperationException>(() => correction.Decide(ApprovalOutcome.Approved, DateTimeOffset.UtcNow, 2));
    }

    [Test]
    public void ApprovedCorrectionCanBeAppliedOnlyOnce()
    {
        var correction = FieldAccountabilityCorrection.ForApplication(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            Guid.NewGuid(), 3, "Replace the confirmation", "manager", "AUTOTEST-P5C-apply", DateTimeOffset.UtcNow);
        correction.Decide(ApprovalOutcome.Approved, DateTimeOffset.UtcNow, 1);

        correction.MarkApplied(DateTimeOffset.UtcNow, 2);

        correction.Status.ShouldBe(FieldAccountabilityCorrectionStatus.Applied);
        Should.Throw<InvalidOperationException>(() => correction.MarkApplied(DateTimeOffset.UtcNow, 3));
    }
}
