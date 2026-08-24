using Cane360.Application.Activities;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Activities;

public class UpdatePersonCommandTests
{
    [Test]
    public async Task UpdatesContactDetailsAndReplacesTheCurrentOperationalRole()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var person = farm.AddPerson("Rudo Ncube", "077 111 2222", new DateOnly(2026, 1, 1));
        var previousRole = farm.AssignRole(person, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var repository = Repository(tenant);
        var handler = new UpdatePersonCommandHandler(repository.Object, User());

        var result = await handler.Handle(new UpdatePersonCommand(
            person.Id,
            "Rudo Moyo",
            "077 333 4444",
            "Storekeeper",
            false,
            new DateOnly(2026, 8, 24),
            person.Version), CancellationToken.None);

        person.DisplayName.ShouldBe("Rudo Moyo");
        person.Phone.ShouldBe("077 333 4444");
        previousRole.EffectiveTo.ShouldBe(new DateOnly(2026, 8, 23));
        person.RoleAssignments.Single(assignment => assignment.EffectiveTo is null).Role.ShouldBe(PersonRole.Storekeeper);
        result.Persons.Single().Roles.Single(assignment => assignment.EffectiveTo is null).Role.ShouldBe("Storekeeper");
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task RejectsAStalePersonnelRecordWithoutSaving()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var person = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(person, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var repository = Repository(tenant);
        var handler = new UpdatePersonCommandHandler(repository.Object, User());

        await Should.ThrowAsync<ConflictException>(() => handler.Handle(new UpdatePersonCommand(
            person.Id,
            "Rudo Moyo",
            null,
            "Storekeeper",
            false,
            new DateOnly(2026, 8, 24),
            person.Version + 1), CancellationToken.None));

        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static Mock<IFarmSetupRepository> Repository(Tenant tenant)
    {
        var repository = new Mock<IFarmSetupRepository>();
        repository.Setup(store => store.GetTenantForUserAsync(
            "user-1", It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        return repository;
    }

    private static IUser User()
    {
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        return user.Object;
    }
}
