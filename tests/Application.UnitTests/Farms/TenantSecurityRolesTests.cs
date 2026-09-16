using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Farms;

public sealed class TenantSecurityRolesTests
{
    [Test]
    public void SupervisorIsANamedRole()
    {
        TenantSecurityRoles.Supervisor.ShouldBe("Supervisor");
    }

    [TestCase("FarmManager", true)]
    [TestCase("Supervisor", true)]
    [TestCase("Grower", false)]
    [TestCase("Unknown", false)]
    public void OnlyManagerAndSupervisorAreInvitable(string role, bool expected)
    {
        TenantSecurityRoles.IsInvitable(role).ShouldBe(expected);
    }
}
