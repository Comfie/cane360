using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class InputRequestDomainTests
{
    [Test]
    public void ToleranceRangeIsInclusiveAndEscalatesBothSides()
    {
        var setup = CreateRule(10m, 5m, 10m);
        var planned = setup.Rule.PlannedQuantity(10m);

        setup.Rule.ApprovalFor(95m, planned).ShouldBe(InputApprovalRequirement.FarmManagerOrGrower);
        setup.Rule.ApprovalFor(110m, planned).ShouldBe(InputApprovalRequirement.FarmManagerOrGrower);
        setup.Rule.ApprovalFor(94.999999m, planned).ShouldBe(InputApprovalRequirement.GrowerOnly);
        setup.Rule.ApprovalFor(110.000001m, planned).ShouldBe(InputApprovalRequirement.GrowerOnly);
    }

    [Test]
    public void MaterialEditInvalidatesApprovalAndIncrementsVersion()
    {
        var setup = CreateRule(10m, 5m, 10m);
        var request = InputRequest.Create(setup.TenantId, setup.FarmId, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 22), "grower-user");
        var line = request.AddLine(setup.Item, setup.Rule, 10m, 100m, 200m, 3m, request.Version);
        request.Submit(DateTimeOffset.UtcNow, "submit-1", request.Version);
        request.OpenApproval(request.Version);
        request.Decide(ApprovalOutcome.Approved, null, DateTimeOffset.UtcNow, request.Version);
        var approvedVersion = request.Version;

        request.ChangeLineQuantity(line.Id, 105m, setup.Rule, 0m, request.Version);

        request.Status.ShouldBe(InputRequestStatus.Draft);
        request.Version.ShouldBe(approvedVersion + 1);
        request.RequiresGrower.ShouldBeFalse();
    }

    [Test]
    public void FirstIssueMakesApprovedLineImmutableAndTracksPartialThenFull()
    {
        var setup = CreateRule(10m, 0m, 0m);
        var request = InputRequest.Create(setup.TenantId, setup.FarmId, Guid.NewGuid(),
            Guid.NewGuid(), Guid.NewGuid(), new DateOnly(2026, 8, 22), "grower-user");
        var line = request.AddLine(setup.Item, setup.Rule, 10m, 100m, 200m, 3m, request.Version);
        request.Submit(DateTimeOffset.UtcNow, "submit-2", request.Version);
        request.OpenApproval(request.Version);
        request.Decide(ApprovalOutcome.Approved, null, DateTimeOffset.UtcNow, request.Version);
        request.RecordIssued(40m, request.Version);

        request.Status.ShouldBe(InputRequestStatus.PartiallyIssued);
        Should.Throw<InvalidOperationException>(() => request.ChangeLineQuantity(
            line.Id, 110m, setup.Rule, 40m, request.Version));
        request.RecordIssued(100m, request.Version);
        request.Status.ShouldBe(InputRequestStatus.FullyIssued);
    }

    [Test]
    public void InvalidRateToleranceAndDatesAreRejected()
    {
        var setup = CreateRule(1m, 0m, 0m);
        Should.Throw<InvalidOperationException>(() => InventoryApplicationRule.Create(
            setup.TenantId, setup.FarmId, setup.Item, Guid.NewGuid(),
            new DateOnly(2026, 8, 22), null, ApplicationCoverageBasis.FieldReportingHectares, 0m, 0m, 0m));
        Should.Throw<InvalidOperationException>(() => InventoryApplicationRule.Create(
            setup.TenantId, setup.FarmId, setup.Item, Guid.NewGuid(),
            new DateOnly(2026, 8, 22), null, ApplicationCoverageBasis.FieldReportingHectares, 1m, -1m, 0m));
        Should.Throw<InvalidOperationException>(() => InventoryApplicationRule.Create(
            setup.TenantId, setup.FarmId, setup.Item, Guid.NewGuid(),
            new DateOnly(2026, 8, 22), new DateOnly(2026, 8, 21),
            ApplicationCoverageBasis.FieldReportingHectares, 1m, 0m, 0m));
    }

    [Test]
    public void EffectiveRuleSelectionUsesInclusiveOperationalDates()
    {
        var setup = CreateRule(1m, 0m, 0m);

        setup.Rule.IsEffective(new DateOnly(2026, 1, 1)).ShouldBeTrue();
        setup.Rule.IsEffective(new DateOnly(2025, 12, 31)).ShouldBeFalse();
        setup.Rule.IsEffective(new DateOnly(2099, 12, 31)).ShouldBeTrue();
    }

    private static RuleSetup CreateRule(decimal rate, decimal lower, decimal upper)
    {
        var tenantId = Guid.NewGuid();
        var farmId = Guid.NewGuid();
        var unit = UnitOfMeasure.Create(tenantId, "KG", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(tenantId, farmId, "FERT-1", "Fertiliser",
            InventoryItemCategory.Fertiliser, unit, null, LotTrackingPolicy.Optional, ExpiryPolicy.Optional);
        var rule = InventoryApplicationRule.Create(tenantId, farmId, item, Guid.NewGuid(),
            new DateOnly(2026, 1, 1), null, ApplicationCoverageBasis.FieldReportingHectares,
            rate, lower, upper);
        return new(tenantId, farmId, item, rule);
    }

    private sealed record RuleSetup(
        Guid TenantId, Guid FarmId, InventoryItem Item, InventoryApplicationRule Rule);
}
