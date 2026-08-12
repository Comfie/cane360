using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.FarmSetup;

public class FarmSetupDomainTests
{
    [Test]
    public void CreateFarmBuildsTheGrowerBoundaryAndDefaultStore()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", "+263771234567");

        var farm = CreateFarm(tenant);

        tenant.GrowerProfile.DisplayName.ShouldBe("Tariro Moyo");
        tenant.Memberships.ShouldHaveSingleItem().UserId.ShouldBe("user-1");
        tenant.ActiveFarm.ShouldBeSameAs(farm);
        farm.Code.ShouldBe("GREEN-01");
        farm.Store.Code.ShouldBe("MAIN");
    }

    [Test]
    public void GrowerCannotCreateASecondActiveFarm()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        CreateFarm(tenant);

        Should.Throw<InvalidOperationException>(() => CreateFarm(tenant))
            .Message.ShouldBe("A grower tenant can have only one active farm.");
    }

    [Test]
    public void FieldCodeIsUniqueWithinTheFarmIgnoringCase()
    {
        var farm = CreateFarm(Tenant.CreateForGrower("user-1", "Tariro Moyo", null));
        farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Pivot", null);

        Should.Throw<InvalidOperationException>(() =>
            farm.AddField("a-01", "Duplicate", 8m, null, ReportingAreaSource.Declared, "Furrow", null));
    }

    [Test]
    public void MappedReportingAreaRequiresMappedHectares()
    {
        var farm = CreateFarm(Tenant.CreateForGrower("user-1", "Tariro Moyo", null));

        Should.Throw<InvalidOperationException>(() =>
            farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Mapped, "Pivot", null));
    }

    [Test]
    public void FieldCanHaveOnlyOneCurrentCropCycle()
    {
        var farm = CreateFarm(Tenant.CreateForGrower("user-1", "Tariro Moyo", null));
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Pivot", null);
        OpenPlantCane(field);

        Should.Throw<InvalidOperationException>(() => OpenPlantCane(field))
            .Message.ShouldBe("This field already has a current crop cycle.");
    }

    [Test]
    public void RatoonCropCycleRequiresARatoonNumber()
    {
        var farm = CreateFarm(Tenant.CreateForGrower("user-1", "Tariro Moyo", null));
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Pivot", null);

        Should.Throw<InvalidOperationException>(() => field.OpenCurrentCropCycle(
            CropCycleType.Ratoon,
            null,
            "N14",
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m));
    }

    private static Farm CreateFarm(Tenant tenant) => tenant.CreateFarm(
        "green-01",
        "Green Valley",
        "Plot 4, Triangle Road",
        "Triangle",
        "Outgrower lease",
        120m,
        "Furrow irrigation from estate canal");

    private static CropCycle OpenPlantCane(Field field) => field.OpenCurrentCropCycle(
        CropCycleType.PlantCane,
        null,
        "N14",
        new DateOnly(2026, 8, 1),
        new DateOnly(2027, 7, 1),
        new DateOnly(2027, 8, 31),
        950m);
}
