using Cane360.Application.Administration;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Administration;

public sealed class ManagerAccessTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);

    private static Tenant Build()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var manager = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1));
        var supervisor = farm.AddPerson("Tendai Dube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        return tenant;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Test]
    public async Task ReturnsCandidatesTaggedWithBothRoles()
    {
        var tenant = Build();
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(s => s.GetTenantAdministrationContextForUserAsync(
                "grower-user", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(s => s.GetManagerInvitationsAsync(
                tenant.Id, It.IsAny<Guid>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ManagerInvitation>());
        var repository = new Mock<IAdministrationReadRepository>();
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns("grower-user");

        var service = new AdministrationService(
            farms.Object, repository.Object, inventory.Object, user.Object, new FixedTimeProvider(Now));

        var result = await service.ManagerAccessAsync(CancellationToken.None);

        result.Candidates.ShouldContain(c => c.Role == "FarmManager");
        result.Candidates.ShouldContain(c => c.Role == "Supervisor");
    }
}
