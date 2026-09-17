using Cane360.Application.Common.Interfaces;
using Cane360.Application.Session;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Session;

public sealed class GetSessionQueryTests
{
    private static IUser User(string id)
    {
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns(id);
        return user.Object;
    }

    [Test]
    public async Task ReturnsRoleAndFarmForALinkedSupervisor()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var supervisor = farm.AddPerson("Tendai Dube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        tenant.AddMembership("supervisor-user", supervisor.Id, TenantSecurityRoles.Supervisor);

        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(s => s.GetTenantForUserAsync("supervisor-user", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var handler = new GetSessionQueryHandler(farms.Object, User("supervisor-user"));

        var result = await handler.Handle(new GetSessionQuery(), CancellationToken.None);

        result.HasTenant.ShouldBeTrue();
        result.Role.ShouldBe("Supervisor");
        result.FarmName.ShouldBe("Green Valley");
    }

    [Test]
    public async Task ReturnsNoTenantForAnUnlinkedAccount()
    {
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(s => s.GetTenantForUserAsync("new-user", It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        var handler = new GetSessionQueryHandler(farms.Object, User("new-user"));

        var result = await handler.Handle(new GetSessionQuery(), CancellationToken.None);

        result.HasTenant.ShouldBeFalse();
        result.Role.ShouldBeNull();
    }
}
