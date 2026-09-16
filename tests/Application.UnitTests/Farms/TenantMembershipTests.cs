using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Farms;

public sealed class TenantMembershipTests
{
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

    [Test]
    public void AddsFarmManagerAndSupervisorMemberships()
    {
        var (tenant, manager, supervisor) = Build();
        tenant.AddMembership("manager-user", manager.Id, TenantSecurityRoles.FarmManager);
        tenant.AddMembership("supervisor-user", supervisor.Id, TenantSecurityRoles.Supervisor);

        tenant.Memberships.Count(m => m.Status == RecordStatus.Active).ShouldBe(3); // grower + 2
        tenant.Memberships.ShouldContain(m => m.SecurityRole == "Supervisor" && m.Status == RecordStatus.Active);
    }

    [Test]
    public void AllowsOnlyOneActiveFarmManager()
    {
        var (tenant, manager, _) = Build();
        var farm = tenant.ActiveFarm!;
        var second = farm.AddPerson("Chipo Banda", null, new DateOnly(2026, 1, 1));
        // isPrimary: false — Farm.AssignRole itself only allows one current *primary* FarmManager;
        // this test targets the separate Tenant-level "one active FarmManager membership" guard.
        farm.AssignRole(second, PersonRole.FarmManager, false, new DateOnly(2026, 1, 1));
        tenant.AddMembership("manager-user", manager.Id, TenantSecurityRoles.FarmManager);

        Should.Throw<InvalidOperationException>(() =>
            tenant.AddMembership("manager-user-2", second.Id, TenantSecurityRoles.FarmManager));
    }

    [Test]
    public void DisableArchivesASupervisorMembership()
    {
        var (tenant, _, supervisor) = Build();
        var membership = tenant.AddMembership("supervisor-user", supervisor.Id, TenantSecurityRoles.Supervisor);
        tenant.DisableMembership(membership.Id);
        membership.Status.ShouldBe(RecordStatus.Archived);
    }
}
