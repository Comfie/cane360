using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Application.FarmSetup;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.FarmSetup;

public class FarmSetupCommandTests
{
    [Test]
    public async Task CreateGrowerFarmCreatesOneAtomicTenantGraph()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        repository
            .Setup(store => store.GetTenantForUserAsync("user-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var handler = new CreateGrowerFarmCommandHandler(repository.Object, user.Object);
        var result = await handler.Handle(ValidFarmCommand(), CancellationToken.None);

        result.IsConfigured.ShouldBeTrue();
        result.Farm!.Name.ShouldBe("Green Valley");
        repository.Verify(store => store.Add(It.Is<Tenant>(tenant =>
            tenant.ActiveFarm != null && tenant.Memberships.Count == 1)), Times.Once);
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task CreateGrowerFarmRejectsAnExistingGrowerTenant()
    {
        var repository = new Mock<IFarmSetupRepository>();
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        repository
            .Setup(store => store.GetTenantForUserAsync("user-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant.CreateForGrower("user-1", "Existing Grower", null));

        var handler = new CreateGrowerFarmCommandHandler(repository.Object, user.Object);

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
            handler.Handle(ValidFarmCommand(), CancellationToken.None));
        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task UpdateFarmInformationUpdatesTheGrowerAndActiveFarm()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", "+263771234567");
        tenant.CreateFarm(
            "GREEN-01",
            "Green Valley",
            "Plot 4, Triangle Road",
            "Triangle",
            "Outgrower lease",
            120m,
            "Furrow irrigation from estate canal");
        var repository = new Mock<IFarmSetupRepository>();
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        repository
            .Setup(store => store.GetTenantForUserAsync("user-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new UpdateFarmInformationCommandHandler(repository.Object, user.Object);

        var result = await handler.Handle(new UpdateFarmInformationCommand(
            "Tariro Ncube",
            "+263779876543",
            "GREEN-02",
            "Green Valley East",
            "Plot 6, Triangle Road",
            "Hippo Valley",
            "Owned",
            145.5m,
            "Centre pivot supplied from the farm dam"), CancellationToken.None);

        result.Grower!.DisplayName.ShouldBe("Tariro Ncube");
        result.Grower.Phone.ShouldBe("+263779876543");
        result.Farm!.Code.ShouldBe("GREEN-02");
        result.Farm.Name.ShouldBe("Green Valley East");
        result.Farm.DeclaredHectares.ShouldBe(145.5m);
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public void FieldValidatorRequiresMappedAreaForMappedReporting()
    {
        var validator = new CreateFieldCommandValidator();
        var command = new CreateFieldCommand(
            "A-01",
            "North block",
            12.5m,
            null,
            "Mapped",
            "Pivot",
            null);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateFieldCommand.MappedHectares));
    }

    [Test]
    public void CropCycleValidatorRejectsRatoonWithoutNumber()
    {
        var validator = new CreateCropCycleCommandValidator();
        var command = new CreateCropCycleCommand(
            Guid.NewGuid(),
            "Ratoon",
            null,
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31),
            950m);

        var result = validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == nameof(CreateCropCycleCommand.RatoonNumber));
    }

    private static CreateGrowerFarmCommand ValidFarmCommand() => new(
        "Tariro Moyo",
        "+263771234567",
        "GREEN-01",
        "Green Valley",
        "Plot 4, Triangle Road",
        "Triangle",
        "Outgrower lease",
        120m,
        "Furrow irrigation from estate canal");
}
