using Cane360.Domain.Farms;
using Cane360.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.FarmSetup;

public class FarmSetupChangeTrackingTests
{
    [Test]
    public void AddingFieldToTrackedFarmMarksOnlyTheFieldAsAdded()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=change-tracking-only;Username=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = tenant.CreateFarm(
            "GREEN-01",
            "Green Valley",
            "Plot 4, Triangle Road",
            "Triangle",
            "Outgrower lease",
            120m,
            "Furrow irrigation from estate canal");
        context.Attach(tenant);

        var field = farm.AddField(
            "A-01",
            "North block",
            3m,
            2.35m,
            ReportingAreaSource.Declared,
            "Furrow",
            "Note now");
        context.ChangeTracker.DetectChanges();

        context.Entry(field).State.ShouldBe(EntityState.Added);
        context.Entry(farm).State.ShouldBe(EntityState.Unchanged);
        context.Entry(tenant).State.ShouldBe(EntityState.Unchanged);
    }

    [Test]
    public void CreatingCropCycleDraftMarksTheLifecycleGraphAsAdded()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=change-tracking-only;Username=test")
            .Options;
        using var context = new ApplicationDbContext(options);
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = tenant.CreateFarm(
            "GREEN-01",
            "Green Valley",
            "Plot 4, Triangle Road",
            "Triangle",
            "Outgrower lease",
            120m,
            "Furrow irrigation from estate canal");
        var field = farm.AddField(
            "A-01",
            "North block",
            3m,
            null,
            ReportingAreaSource.Declared,
            "Furrow",
            null);
        var variety = tenant.AddCropVariety("N14", "N14");
        context.Attach(tenant);

        var cropCycle = field.CreateCropCycleDraft(
            CropCycleType.PlantCane,
            null,
            variety,
            "N14",
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            95m,
            new DateTimeOffset(2026, 8, 12, 8, 0, 0, TimeSpan.Zero),
            "user-1");
        context.ChangeTracker.DetectChanges();

        context.Entry(cropCycle).State.ShouldBe(EntityState.Added);
        context.Entry(cropCycle.StatusChanges.Single()).State.ShouldBe(EntityState.Added);
        context.Entry(field).State.ShouldBe(EntityState.Unchanged);
        context.Entry(farm).State.ShouldBe(EntityState.Unchanged);
    }
}
