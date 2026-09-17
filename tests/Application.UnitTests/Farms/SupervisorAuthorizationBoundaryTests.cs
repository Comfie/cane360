using Ardalis.GuardClauses;
using Cane360.Application.Activities;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Application.FarmSetup;
using Cane360.Application.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Farms;

/// <summary>
/// A Supervisor membership must resolve only through the operational capture surface. Every other
/// area keeps resolving through <see cref="IFarmSetupRepository.GetTenantForUserAsync"/>, which
/// admits Grower and FarmManager only, so those areas stay closed by construction.
/// </summary>
public sealed class SupervisorAuthorizationBoundaryTests
{
    private const string GrowerUserId = "grower-user";
    private const string ManagerUserId = "manager-user";
    private const string SupervisorUserId = "supervisor-user";

    [Test]
    public async Task SupervisorCannotReachPayroll()
    {
        Fixture fixture = CreateFixture();
        var handler = new CreatePayrollPeriodCommandHandler(fixture.Repository.Object,
            new Mock<IPayrollRepository>(MockBehavior.Strict).Object, User(SupervisorUserId),
            new FixedClock());

        NotFoundException error = await Should.ThrowAsync<NotFoundException>(() =>
            handler.Handle(new CreatePayrollPeriodCommand(2026, 9), CancellationToken.None));

        error.Message.ShouldContain(SupervisorUserId);
        fixture.Repository.Verify(store => store.GetTenantForOperationalUserAsync(
            It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task SupervisorCannotEditFarmConfiguration()
    {
        Fixture fixture = CreateFixture();
        var handler = new UpdateFarmInformationCommandHandler(fixture.Repository.Object,
            User(SupervisorUserId));

        // The handler treats an unresolved tenant as "no farm yet", so denial surfaces as a
        // validation failure rather than a not-found; either way the write never happens.
        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() =>
            handler.Handle(new UpdateFarmInformationCommand("Impostor", null, "GREEN",
                "Renamed Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow"),
                CancellationToken.None));

        fixture.Tenant.ActiveFarm!.Name.ShouldBe("Green Valley");
        fixture.Tenant.GrowerProfile.DisplayName.ShouldBe("Tariro Moyo");
        fixture.Repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SupervisorCannotConfigureActivityTypes()
    {
        Fixture fixture = CreateFixture();
        var handler = new CreateActivityTypeCommandHandler(fixture.Repository.Object,
            User(SupervisorUserId), new FixedClock());

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(
            new CreateActivityTypeCommand("WEED", "Weeding", true, true, "Hectares"),
            CancellationToken.None));

        fixture.Repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SupervisorCannotManagePersonnel()
    {
        Fixture fixture = CreateFixture();
        var handler = new CreatePersonCommandHandler(fixture.Repository.Object,
            User(SupervisorUserId));

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(
            new CreatePersonCommand("New Person", null, new DateOnly(2026, 1, 1), ["Storekeeper"],
                false), CancellationToken.None));

        fixture.Repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task SupervisorCanReadActivities()
    {
        Fixture fixture = CreateFixture();
        var handler = new GetActivitiesQueryHandler(fixture.Repository.Object,
            User(SupervisorUserId));

        ActivityCollectionDto result = await handler.Handle(EmptyActivityQuery(),
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);
        result.Items.Single().Id.ShouldBe(fixture.Activity.Id);
    }

    [Test]
    public async Task SupervisorCanReadFarmAndFieldWorkspace()
    {
        Fixture fixture = CreateFixture();
        var setup = new GetFarmSetupQueryHandler(fixture.Repository.Object, User(SupervisorUserId));
        var cycles = new GetCropCyclesQueryHandler(fixture.Repository.Object, User(SupervisorUserId));

        FarmSetupDto workspace = await setup.Handle(new GetFarmSetupQuery(), CancellationToken.None);
        CropCycleCollectionDto register = await cycles.Handle(
            new GetCropCyclesQuery(fixture.Field.Id), CancellationToken.None);

        workspace.IsConfigured.ShouldBeTrue();
        workspace.Farm!.Fields.Single().Id.ShouldBe(fixture.Field.Id);
        register.Field.Id.ShouldBe(fixture.Field.Id);
    }

    [Test]
    public async Task SupervisorCanCaptureAnActivity()
    {
        Fixture fixture = CreateFixture();
        var handler = new CreateActivityCommandHandler(fixture.Repository.Object,
            User(SupervisorUserId), Identity(), new FixedClock());

        ActivityDetailsDto result = await handler.Handle(new CreateActivityCommand(
            fixture.Field.Id, fixture.Cycle.Id, fixture.Type.Id, "Unplanned", null,
            fixture.Supervisor.Id), CancellationToken.None);

        result.Activity.Kind.ShouldBe("Unplanned");
        fixture.Repository.Verify(store => store.SaveChangesAsync(CancellationToken.None),
            Times.Once);
    }

    [Test]
    public async Task SupervisorCannotCaptureOrReadAnotherSupervisorsActivity()
    {
        Fixture fixture = CreateFixture();
        Farm farm = fixture.Tenant.ActiveFarm!;
        Person other = farm.AddPerson("Other Supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(other, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        Activity foreignActivity = fixture.Cycle.CreateActivity(fixture.Tenant.Id, farm.Id,
            fixture.Field.Id, fixture.Type, ActivityPlanningKind.Planned,
            new DateOnly(2026, 8, 14), other.Id);
        var user = User(SupervisorUserId);

        var list = new GetActivitiesQueryHandler(fixture.Repository.Object, user);
        ActivityCollectionDto visible = await list.Handle(EmptyActivityQuery(), CancellationToken.None);
        visible.Items.Select(item => item.Id).ShouldNotContain(foreignActivity.Id);

        var create = new CreateActivityCommandHandler(fixture.Repository.Object, user,
            Identity(), new FixedClock());
        await Should.ThrowAsync<NotFoundException>(() => create.Handle(
            new CreateActivityCommand(fixture.Field.Id, fixture.Cycle.Id, fixture.Type.Id,
                "Unplanned", null, other.Id), CancellationToken.None));

        var record = new RecordActualWorkCommandHandler(fixture.Repository.Object, user,
            Identity(), new FixedClock());
        await Should.ThrowAsync<NotFoundException>(() => record.Handle(
            new RecordActualWorkCommand(foreignActivity.Id, foreignActivity.Version,
                new DateTimeOffset(2026, 8, 14, 9, 0, 0, TimeSpan.Zero), 1m, null),
            CancellationToken.None));

        fixture.Repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [TestCase(GrowerUserId)]
    [TestCase(ManagerUserId)]
    public async Task GrowerAndFarmManagerKeepTheirActivityAccess(string userId)
    {
        Fixture fixture = CreateFixture();
        var handler = new GetActivitiesQueryHandler(fixture.Repository.Object, User(userId));

        ActivityCollectionDto result = await handler.Handle(EmptyActivityQuery(),
            CancellationToken.None);

        result.TotalCount.ShouldBe(1);
    }

    [TestCase(GrowerUserId)]
    [TestCase(ManagerUserId)]
    public async Task GrowerAndFarmManagerKeepTheirPayrollAccess(string userId)
    {
        Fixture fixture = CreateFixture();
        var payroll = new Mock<IPayrollRepository>();
        var handler = new CreatePayrollPeriodCommandHandler(fixture.Repository.Object,
            payroll.Object, User(userId), new FixedClock());

        PayrollPeriodDto result = await handler.Handle(new CreatePayrollPeriodCommand(2026, 9),
            CancellationToken.None);

        result.Year.ShouldBe(2026);
        payroll.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task GrowerKeepsFarmConfigurationAccess()
    {
        Fixture fixture = CreateFixture();
        var handler = new UpdateFarmInformationCommandHandler(fixture.Repository.Object,
            User(GrowerUserId));

        await handler.Handle(new UpdateFarmInformationCommand("Tariro Moyo", null, "GREEN",
            "Renamed Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow"),
            CancellationToken.None);

        fixture.Tenant.ActiveFarm!.Name.ShouldBe("Renamed Valley");
    }

    private static GetActivitiesQuery EmptyActivityQuery() =>
        new(null, null, null, null, null, null);

    /// <summary>
    /// Applies the production role predicates so a test proves which repository method a handler
    /// calls, not merely that it propagates a null the test itself stubbed in.
    /// </summary>
    private static Fixture CreateFixture()
    {
        DateTimeOffset now = new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);
        Tenant tenant = Tenant.CreateForGrower(GrowerUserId, "Tariro Moyo", null);
        CropVariety variety = tenant.AddCropVariety("N14", "N14");
        ActivityType type = tenant.AddActivityType("SPRAY", "Foliar spray", true, true,
            ActivityQuantityBasis.Hectares);
        Farm farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease",
            120m, "Furrow");

        Person manager = farm.AddPerson("Rutendo Chari", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1));
        tenant.AddMembership(ManagerUserId, manager.Id, TenantSecurityRoles.FarmManager);

        Person supervisor = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        tenant.AddMembership(SupervisorUserId, supervisor.Id, TenantSecurityRoles.Supervisor);

        Field field = farm.AddField("A-01", "North block", 12.5m, null,
            ReportingAreaSource.Declared, "Furrow", null);
        CropCycle cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety,
            variety.Name, new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 1),
            new DateOnly(2027, 8, 31), 900m, now, GrowerUserId);
        field.ActivateCropCycle(cycle, now, GrowerUserId);
        Activity activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), supervisor.Id);

        var repository = new Mock<IFarmSetupRepository>();
        repository.Setup(store => store.GetTenantForUserAsync(It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, bool _, CancellationToken _) =>
                Resolves(tenant, userId, includeSupervisor: false) ? tenant : null);
        repository.Setup(store => store.GetTenantForOperationalUserAsync(It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, bool _, CancellationToken _) =>
                Resolves(tenant, userId, includeSupervisor: true) ? tenant : null);
        repository.Setup(store => store.GetTenantWorkspaceForUserAsync(It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, CancellationToken _) =>
                Resolves(tenant, userId, includeSupervisor: true) ? tenant : null);
        repository.Setup(store => store.GetFarmSettingsAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        return new Fixture(repository, tenant, field, cycle, type, supervisor, activity);
    }

    /// <summary>Mirrors the membership predicates in <c>FarmSetupRepository</c>.</summary>
    private static bool Resolves(Tenant tenant, string userId, bool includeSupervisor) =>
        tenant.Memberships.Any(membership =>
            membership.UserId == userId &&
            membership.Status == RecordStatus.Active &&
            (membership.SecurityRole == TenantSecurityRoles.Grower ||
             membership.SecurityRole == TenantSecurityRoles.FarmManager ||
             (includeSupervisor && membership.SecurityRole == TenantSecurityRoles.Supervisor)));

    private static IUser User(string id)
    {
        var user = new Mock<IUser>();
        user.SetupGet(current => current.Id).Returns(id);
        user.SetupGet(current => current.CorrelationId).Returns("supervisor-boundary-test");
        return user.Object;
    }

    private static IIdentityService Identity()
    {
        var identity = new Mock<IIdentityService>();
        identity.Setup(service => service.GetUserNameAsync(It.IsAny<string>()))
            .ReturnsAsync("member@example.test");
        return identity.Object;
    }

    private sealed record Fixture(
        Mock<IFarmSetupRepository> Repository,
        Tenant Tenant,
        Field Field,
        CropCycle Cycle,
        ActivityType Type,
        Person Supervisor,
        Activity Activity);

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);
    }
}
