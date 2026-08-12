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
    public void FieldCanHaveOnlyOneActiveOrReadyCropCycle()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = CreateFarm(tenant);
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Pivot", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var first = CreatePlantCaneDraft(field, variety);
        var second = CreatePlantCaneDraft(field, variety);
        field.ActivateCropCycle(first, Now, "user-1");

        Should.Throw<InvalidOperationException>(() => field.ActivateCropCycle(second, Now, "user-1"))
            .Message.ShouldBe("This field already has an Active or Ready-for-harvest crop cycle.");
    }

    [Test]
    public void RatoonCropCycleRequiresARatoonNumber()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = CreateFarm(tenant);
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Pivot", null);
        var variety = tenant.AddCropVariety("N14", "N14");

        Should.Throw<InvalidOperationException>(() => field.CreateCropCycleDraft(
            CropCycleType.Ratoon,
            null,
            variety,
            "N14",
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m,
            Now,
            "user-1"));
    }

    private static Farm CreateFarm(Tenant tenant) => tenant.CreateFarm(
        "green-01",
        "Green Valley",
        "Plot 4, Triangle Road",
        "Triangle",
        "Outgrower lease",
        120m,
        "Furrow irrigation from estate canal");

    private static readonly DateTimeOffset Now = new(2026, 8, 12, 8, 0, 0, TimeSpan.Zero);

    private static CropCycle CreatePlantCaneDraft(Field field, CropVariety variety) => field.CreateCropCycleDraft(
        CropCycleType.PlantCane,
        null,
        variety,
        "N14",
        new DateOnly(2026, 8, 1),
        new DateOnly(2027, 7, 1),
        new DateOnly(2027, 8, 31),
        950m,
        Now,
        "user-1");
}
