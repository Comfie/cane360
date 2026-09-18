using Ardalis.GuardClauses;
using Cane360.Application.Administration;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using Cane360.Domain.Auditing;
using Cane360.Domain.MillRecords;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Administration;

public sealed class AdministrationServiceTests
{
    [Test]
    public async Task GrowerCanListTenantUsers()
    {
        Tenant tenant = TenantWithFarm();
        var reads = new Mock<IAdministrationReadRepository>();
        reads.Setup(item => item.GetUsersAsync(tenant.Id, tenant.ActiveFarm!.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new AdministrationUserDto(Guid.NewGuid(), "grower", "owner@example.test",
                TenantSecurityRoles.Grower, "Active", null, null)]);
        AdministrationService service = Service(tenant, "grower", reads);

        IReadOnlyList<AdministrationUserDto> users = await service.UsersAsync(CancellationToken.None);

        users.Count.ShouldBe(1);
        reads.Verify(item => item.GetUsersAsync(tenant.Id, tenant.ActiveFarm!.Id,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void CrossTenantUserListReturnsNoUsableData()
    {
        Tenant tenant = TenantWithFarm();
        var reads = new Mock<IAdministrationReadRepository>();
        AdministrationService service = Service(tenant, "other-user", reads);

        Should.Throw<NotFoundException>(async () => await service.UsersAsync(CancellationToken.None));
        reads.Verify(item => item.GetUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void FarmManagerCannotInspectUserMembershipsOrInvitationTokens()
    {
        Tenant tenant = TenantWithFarm();
        var person = tenant.ActiveFarm!.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        tenant.AddMembership("manager", person.Id, TenantSecurityRoles.FarmManager);
        var reads = new Mock<IAdministrationReadRepository>();
        AdministrationService service = Service(tenant, "manager", reads);

        Should.Throw<ForbiddenAccessException>(async () =>
            await service.UsersAsync(CancellationToken.None));
        Should.Throw<ForbiddenAccessException>(async () =>
            await service.ManagerAccessAsync(CancellationToken.None));
        reads.Verify(item => item.GetUsersAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task AuditQueryIsTenantScopedAndPaginated()
    {
        Tenant tenant = TenantWithFarm();
        var reads = new Mock<IAdministrationReadRepository>();
        AdministrationAuditFilter filter = new(null, null, null, null, null, null, null, 2, 25);
        reads.Setup(item => item.GetAuditAsync(tenant.Id, tenant.ActiveFarm!.Id, filter,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AdministrationAuditPageDto(2, 25, 31, []));
        AdministrationService service = Service(tenant, "grower", reads);

        AdministrationAuditPageDto page = await service.AuditAsync(filter, CancellationToken.None);

        page.Page.ShouldBe(2);
        page.TotalCount.ShouldBe(31);
        reads.Verify(item => item.GetAuditAsync(tenant.Id, tenant.ActiveFarm!.Id,
            filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void FarmManagerCannotDisableGrower()
    {
        Tenant tenant = TenantWithFarm();
        var person = tenant.ActiveFarm!.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        tenant.AddMembership("manager", person.Id, TenantSecurityRoles.FarmManager);
        AdministrationService service = Service(tenant, "manager", new Mock<IAdministrationReadRepository>());

        Should.Throw<ForbiddenAccessException>(async () =>
            await service.DisableManagerAsync(tenant.Memberships.Single(item =>
                item.SecurityRole == TenantSecurityRoles.Grower).Id, CancellationToken.None));
    }

    [Test]
    public async Task GrowerDisableReturnsUpdatedTenantMembership()
    {
        Tenant tenant = TenantWithFarm();
        var person = tenant.ActiveFarm!.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        var membership = tenant.AddMembership("manager", person.Id, TenantSecurityRoles.FarmManager);
        var reads = new Mock<IAdministrationReadRepository>();
        var expected = new AdministrationUserDto(membership.Id, "manager", "manager@example.test",
            TenantSecurityRoles.FarmManager, "Archived", person.Id, person.DisplayName);
        reads.Setup(item => item.GetUsersAsync(tenant.Id, tenant.ActiveFarm.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync([expected]);
        AdministrationService service = Service(tenant, "grower", reads);

        AdministrationUserDto result = await service.DisableManagerAsync(membership.Id,
            CancellationToken.None);

        result.ShouldBeSameAs(expected);
        membership.Status.ToString().ShouldBe("Archived");
        reads.Verify(item => item.GetUsersAsync(tenant.Id, tenant.ActiveFarm.Id,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void TenantCannotLoseRequiredGrowerAuthority()
    {
        Tenant tenant = TenantWithFarm();
        Guid ownerId = tenant.Memberships.Single().Id;

        Should.Throw<InvalidOperationException>(() => tenant.DisableMembership(ownerId));
        tenant.Memberships.Single().Status.ShouldBe(RecordStatus.Active);
    }

    [Test]
    public async Task AuditExportUsesCurrentFiltersAndCsvInjectionProtectionAndCreatesAuditFact()
    {
        Tenant tenant = TenantWithFarm();
        var reads = new Mock<IAdministrationReadRepository>();
        AdministrationAuditFilter filter = new(null, null, "=SUM(1)", null,
            null, null, null, 1, 25);
        reads.Setup(item => item.GetAuditAsync(tenant.Id, tenant.ActiveFarm!.Id,
            It.Is<AdministrationAuditFilter>(value => value.Action == filter.Action),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tenantId, Guid farmId, AdministrationAuditFilter value,
                CancellationToken cancellationToken) =>
                new AdministrationAuditPageDto(value.Page, value.PageSize, 1,
                [new AdministrationAuditDto(Guid.NewGuid(), DateTimeOffset.UtcNow,
                    "Test", Guid.NewGuid(), "=SUM(1)", "grower", null,
                    null, null, "@sensitive-looking", null, "trace")]));
        AuditEvent? recorded = null;
        reads.Setup(item => item.RecordExportAsync(It.IsAny<AuditEvent>(),
            It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((item, _) => recorded = item)
            .Returns(Task.CompletedTask);
        AdministrationService service = Service(tenant, "grower", reads);

        AdministrationAuditExportDto export = await service.ExportAuditAsync(filter,
            CancellationToken.None);

        export.Content.ShouldContain("'=SUM(1)");
        export.Content.ShouldContain("'@sensitive-looking");
        recorded.ShouldNotBeNull();
        recorded.Action.ShouldBe("Exported");
        recorded.TenantId.ShouldBe(tenant.Id);
    }

    [Test]
    public void DocumentCategoryCodeIsTenantUnique()
    {
        Tenant tenant = TenantWithFarm();
        var reads = new Mock<IAdministrationReadRepository>();
        reads.Setup(item => item.GetCategoriesAsync(tenant.Id, false,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([DocumentCategory.Create(tenant.Id, "MILL", "Mill", null)]);
        AdministrationService service = Service(tenant, "grower", reads);

        Should.Throw<ConflictException>(async () => await service.CreateCategoryAsync(
            " mill ", "Duplicate", null, CancellationToken.None));
        reads.Verify(item => item.Add(It.IsAny<DocumentCategory>()), Times.Never);
    }

    [Test]
    public void SettingEffectiveRangesCannotOverlap()
    {
        Tenant tenant = TenantWithFarm();
        var farms = Farms(tenant, "grower");
        farms.Setup(item => item.GetFarmSettingsAsync(tenant.Id, tenant.ActiveFarm!.Id,
            false, It.IsAny<CancellationToken>())).ReturnsAsync([
                FarmSetting.Create(tenant.Id, tenant.ActiveFarm!.Id,
                    FarmSetting.ActivityLateEntryReasonDays, 2,
                    new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31))]);
        var user = new Mock<IUser>();
        user.SetupGet(item => item.Id).Returns("grower");
        var service = new AdministrationService(farms.Object,
            new Mock<IAdministrationReadRepository>().Object,
            new Mock<IInventoryRepository>().Object, user.Object, TimeProvider.System);

        Should.Throw<ConflictException>(async () => await service.CreateSettingAsync(
            FarmSetting.ActivityLateEntryReasonDays, 3,
            new DateOnly(2026, 11, 1), null, CancellationToken.None));
    }

    private static Tenant TenantWithFarm()
    {
        Tenant tenant = Tenant.CreateForGrower("grower", "Grower", null);
        tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Owned", 10, "None");
        return tenant;
    }

    private static AdministrationService Service(Tenant tenant, string userId,
        Mock<IAdministrationReadRepository> reads)
    {
        var farms = Farms(tenant, userId);
        var inventory = new Mock<IInventoryRepository>();
        var user = new Mock<IUser>();
        user.SetupGet(item => item.Id).Returns(userId);
        return new AdministrationService(farms.Object, reads.Object, inventory.Object,
            user.Object, TimeProvider.System);
    }

    private static Mock<IFarmSetupRepository> Farms(Tenant tenant, string userId)
    {
        var farms = new Mock<IFarmSetupRepository>();
        farms.Setup(item => item.GetTenantAdministrationContextForUserAsync(userId,
            It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant.Memberships.Any(item => item.UserId == userId) ? tenant : null);
        return farms;
    }
}
