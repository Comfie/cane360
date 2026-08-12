using Ardalis.GuardClauses;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.CropCycles;

public class CropCycleCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);

    [Test]
    public async Task CreateDraftUsesOnlyTheAuthenticatedTenantVariety()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = CurrentUser();
        var tenant = CreateTenant(out var field, out var variety);
        repository.Setup(store => store.GetTenantForUserAsync("user-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new CreateCropCycleCommandHandler(repository.Object, user.Object, new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateCropCycleCommand(
            field.Id,
            "PlantCane",
            null,
            variety.Id,
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m), CancellationToken.None);

        result.CropCycle.Status.ShouldBe("Draft");
        result.CropCycle.Variety.ShouldBe("N14");
        result.Timeline.ShouldContain(item => item.Title == "Cycle recorded as Draft");
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task CrossTenantFieldIsReportedAsNotFound()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = CurrentUser();
        var tenant = CreateTenant(out _, out var variety);
        repository.Setup(store => store.GetTenantForUserAsync("user-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new CreateCropCycleCommandHandler(repository.Object, user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(new CreateCropCycleCommand(
            Guid.NewGuid(),
            "PlantCane",
            null,
            variety.Id,
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m), CancellationToken.None));

        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StaleTransitionVersionReturnsConflictWithoutSaving()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = CurrentUser();
        var tenant = CreateTenant(out var field, out var variety);
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
        repository.Setup(store => store.GetTenantForUserAsync("user-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new ActivateCropCycleCommandHandler(repository.Object, user.Object, new FixedTimeProvider(Now));

        await Should.ThrowAsync<ConflictException>(() => handler.Handle(
            new ActivateCropCycleCommand(field.Id, cycle.Id, 12), CancellationToken.None));

        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task QueryCannotSeeAFieldWithoutAnActiveMembership()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = CurrentUser();
        repository.Setup(store => store.GetTenantForUserAsync("user-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        var handler = new GetCropCyclesQueryHandler(repository.Object, user.Object);

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(
            new GetCropCyclesQuery(Guid.NewGuid()), CancellationToken.None));
    }

    private static Mock<IUser> CurrentUser()
    {
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        return user;
    }

    private static Tenant CreateTenant(out Field field, out CropVariety variety)
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        variety = tenant.AddCropVariety("N14", "N14");
        var farm = tenant.CreateFarm(
            "GREEN-01", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        field = farm.AddField(
            "A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Furrow", null);
        return tenant;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
