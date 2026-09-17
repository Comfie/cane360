using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class ManagerInvitationCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);

    private static (Tenant Tenant, Person Manager, Person Supervisor) Build()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var manager = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1));
        var supervisor = farm.AddPerson("Tendai Dube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        return (tenant, manager, supervisor);
    }

    private static (Mock<IFarmSetupRepository> Farms, Mock<IInventoryRepository> Inventory) Repos(Tenant tenant)
    {
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(s => s.GetTenantForUserAsync("grower-user", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var inventory = new Mock<IInventoryRepository>();
        return (farms, inventory);
    }

    private static IUser User()
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns("grower-user");
        return user.Object;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public async Task CreatesASupervisorInvitationForASupervisorPerson()
    {
        var (tenant, _, supervisor) = Build();
        var (farms, inventory) = Repos(tenant);
        ManagerInvitation? captured = null;
        inventory.Setup(s => s.Add(It.IsAny<ManagerInvitation>()))
            .Callback<ManagerInvitation>(i => captured = i);
        var handler = new CreateManagerInvitationCommandHandler(
            farms.Object, inventory.Object, User(), new FixedTimeProvider(Now));

        var result = await handler.Handle(
            new CreateManagerInvitationCommand(supervisor.Id, 48, TenantSecurityRoles.Supervisor),
            CancellationToken.None);

        result.Token.ShouldNotBeNullOrWhiteSpace();
        captured!.SecurityRole.ShouldBe("Supervisor");
        inventory.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task RejectsSupervisorRoleWhenPersonIsNotASupervisor()
    {
        var (tenant, manager, _) = Build();
        var (farms, inventory) = Repos(tenant);
        var handler = new CreateManagerInvitationCommandHandler(
            farms.Object, inventory.Object, User(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
            handler.Handle(new CreateManagerInvitationCommand(manager.Id, 48, TenantSecurityRoles.Supervisor),
                CancellationToken.None));
        inventory.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RejectsANonInvitableRole()
    {
        var (tenant, manager, _) = Build();
        var (farms, inventory) = Repos(tenant);
        var handler = new CreateManagerInvitationCommandHandler(
            farms.Object, inventory.Object, User(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
            handler.Handle(new CreateManagerInvitationCommand(manager.Id, 48, TenantSecurityRoles.Grower),
                CancellationToken.None));
    }
}
