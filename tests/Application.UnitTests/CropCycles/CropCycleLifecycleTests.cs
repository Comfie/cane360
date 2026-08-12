using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.CropCycles;

public class CropCycleLifecycleTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);

    [Test]
    public void ValidLifecycleRecordsHarvestAndCanClose()
    {
        var (field, cycle) = CreateDraft();

        field.ActivateCropCycle(cycle, Now, "user-1");
        cycle.MarkReadyForHarvest(Now.AddDays(300), "user-1");
        cycle.RecordHarvest(
            new DateOnly(2027, 7, 10),
            824.5m,
            new DateOnly(2027, 7, 11),
            Now.AddDays(333),
            "user-1");
        cycle.Close(Now.AddDays(334), "user-1");

        cycle.Status.ShouldBe(CropCycleStatus.Closed);
        cycle.HarvestResult.ShouldNotBeNull();
        cycle.HarvestResult.ActualTonnes.ShouldBe(824.5m);
        cycle.StatusChanges.Select(change => change.ToStatus).ShouldBe([
            CropCycleStatus.Draft,
            CropCycleStatus.Active,
            CropCycleStatus.ReadyForHarvest,
            CropCycleStatus.Harvested,
            CropCycleStatus.Closed]);
    }

    [Test]
    public void HarvestRequiresPositiveTonnes()
    {
        var (field, cycle) = CreateDraft();
        field.ActivateCropCycle(cycle, Now, "user-1");
        cycle.MarkReadyForHarvest(Now, "user-1");

        Should.Throw<ArgumentOutOfRangeException>(() => cycle.RecordHarvest(
            new DateOnly(2027, 7, 10), 0, new DateOnly(2027, 7, 11), Now, "user-1"));
        cycle.Status.ShouldBe(CropCycleStatus.ReadyForHarvest);
        cycle.HarvestResult.ShouldBeNull();
    }

    [Test]
    public void HarvestRejectsFutureDate()
    {
        var (field, cycle) = CreateDraft();
        field.ActivateCropCycle(cycle, Now, "user-1");
        cycle.MarkReadyForHarvest(Now, "user-1");

        Should.Throw<InvalidOperationException>(() => cycle.RecordHarvest(
            new DateOnly(2027, 7, 12), 800, new DateOnly(2027, 7, 11), Now, "user-1"));
    }

    [Test]
    public void ClosedCycleRejectsFurtherTransitions()
    {
        var (field, cycle) = CreateDraft();
        field.ActivateCropCycle(cycle, Now, "user-1");
        cycle.MarkReadyForHarvest(Now, "user-1");
        cycle.RecordHarvest(
            new DateOnly(2027, 7, 10), 800, new DateOnly(2027, 7, 11), Now, "user-1");
        cycle.Close(Now, "user-1");

        Should.Throw<InvalidOperationException>(() => cycle.Close(Now, "user-1"));
        Should.Throw<InvalidOperationException>(() => cycle.MarkReadyForHarvest(Now, "user-1"));
    }

    [Test]
    public void CancelledCycleIsTerminalAndKeepsReason()
    {
        var (_, cycle) = CreateDraft();

        cycle.Cancel("Replanting plan changed", Now, "user-1");

        cycle.Status.ShouldBe(CropCycleStatus.Cancelled);
        cycle.StatusChanges.Last().Reason.ShouldBe("Replanting plan changed");
        Should.Throw<InvalidOperationException>(() => cycle.MarkReadyForHarvest(Now, "user-1"));
    }

    private static (Field Field, CropCycle Cycle) CreateDraft()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var farm = tenant.CreateFarm(
            "GREEN-01", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var field = farm.AddField(
            "A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(
            CropCycleType.PlantCane,
            null,
            variety,
            variety.Name,
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m,
            Now,
            "user-1");

        return (field, cycle);
    }
}
