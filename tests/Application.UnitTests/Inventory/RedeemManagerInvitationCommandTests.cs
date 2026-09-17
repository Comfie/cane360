using System.Security.Cryptography;
using System.Text;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class RedeemManagerInvitationCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 9, 0, 0, TimeSpan.Zero);
    private const string Token = "supervisor-token-value";

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    [Test]
    public async Task RedeemingASupervisorInvitationCreatesASupervisorSession()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Tariro Moyo", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var supervisor = farm.AddPerson("Tendai Dube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var invitation = ManagerInvitation.Create(tenant.Id, farm.Id, supervisor.Id, Hash(Token),
            Now.AddHours(1), "grower-user", TenantSecurityRoles.Supervisor);

        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(s => s.GetTenantAsync(tenant.Id, It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(s => s.GetManagerInvitationByHashAsync(Hash(Token), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(invitation);
        var user = new Mock<IUser>();
        user.Setup(u => u.Id).Returns("supervisor-user");

        var handler = new RedeemManagerInvitationCommandHandler(
            farms.Object, inventory.Object, user.Object, new FixedTimeProvider(Now));

        var session = await handler.Handle(new RedeemManagerInvitationCommand(Token), CancellationToken.None);

        session.SecurityRole.ShouldBe("Supervisor");
        session.PersonId.ShouldBe(supervisor.Id);
        tenant.Memberships.ShouldContain(m => m.UserId == "supervisor-user" && m.SecurityRole == "Supervisor");
        inventory.Verify(s => s.SaveChangesAsync(CancellationToken.None), Times.Once);
    }
}
