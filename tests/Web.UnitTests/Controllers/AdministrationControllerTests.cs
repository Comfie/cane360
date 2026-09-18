using Cane360.Application.Administration;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using Cane360.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class AdministrationControllerTests
{
    [Test]
    public async Task DisableManagerReturnsUpdatedMembership()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10, "None");
        var person = tenant.ActiveFarm!.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        var membership = tenant.AddMembership("manager", person.Id, TenantSecurityRoles.FarmManager);
        var expected = new AdministrationUserDto(membership.Id, "manager", "manager@example.test",
            TenantSecurityRoles.FarmManager, "Archived", person.Id, person.DisplayName);
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(value => value.GetTenantAdministrationContextForUserAsync("grower", true,
            It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        var reads = new Mock<IAdministrationReadRepository>();
        reads.Setup(value => value.GetUsersAsync(tenant.Id, tenant.ActiveFarm.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync([expected]);
        var user = new Mock<IUser>();
        user.SetupGet(value => value.Id).Returns("grower");
        var service = new AdministrationService(farms.Object, reads.Object,
            new Mock<IInventoryRepository>().Object, user.Object, TimeProvider.System);

        var result = await new AdministrationController(service).DisableManager(membership.Id,
            CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }
}
