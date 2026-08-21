using Ardalis.GuardClauses;
using Cane360.Application.Activities;
using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Activities;

public class ActivityCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);

    [TestCase("Planned")]
    [TestCase("Unplanned")]
    public async Task CreatesPlannedAndUnplannedActivitiesInsideAuthenticatedTenant(string kind)
    {
        var context = CreateContext();
        var repository = Repository(context.Tenant);
        var handler = new CreateActivityCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateActivityCommand(
            context.Field.Id,
            context.Cycle.Id,
            context.Type.Id,
            kind,
            kind == "Planned" ? new DateOnly(2026, 8, 13) : null,
            context.Supervisor.Id), CancellationToken.None);

        result.Activity.Kind.ShouldBe(kind);
        result.Activity.QuantityBasis.ShouldBe("Hectares");
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task RejectsCycleFromAnotherFieldWithoutSaving()
    {
        var context = CreateContext();
        var otherField = context.Farm.AddField(
            "B-01", "South block", 4m, null, ReportingAreaSource.Declared, "Furrow", null);
        var repository = Repository(context.Tenant);
        var handler = new CreateActivityCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<NotFoundException>(() => handler.Handle(new CreateActivityCommand(
            otherField.Id,
            context.Cycle.Id,
            context.Type.Id,
            "Planned",
            new DateOnly(2026, 8, 13),
            context.Supervisor.Id), CancellationToken.None));

        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task RejectsSupervisorWithoutEffectiveRole()
    {
        var context = CreateContext();
        var person = context.Farm.AddPerson("Tendai Dube", null, new DateOnly(2026, 1, 1));
        var repository = Repository(context.Tenant);
        var handler = new CreateActivityCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => handler.Handle(
            new CreateActivityCommand(
                context.Field.Id,
                context.Cycle.Id,
                context.Type.Id,
                "Planned",
                new DateOnly(2026, 8, 13),
                person.Id), CancellationToken.None));
        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task StaleActualWorkVersionReturnsConflictWithoutSaving()
    {
        var context = CreateContext();
        var activity = context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), context.Supervisor.Id);
        var repository = Repository(context.Tenant);
        var handler = new RecordActualWorkCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<ConflictException>(() => handler.Handle(new RecordActualWorkCommand(
            activity.Id, 99, Now.AddHours(-1), 2m, null), CancellationToken.None));
        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase("2026-08-12T08:30:00Z", "2026-08-12T08:30:00+00:00")]
    [TestCase("2026-08-12T10:30:00+02:00", "2026-08-12T08:30:00+00:00")]
    [TestCase("2026-08-12T03:30:00-05:00", "2026-08-12T08:30:00+00:00")]
    [TestCase("2026-08-11T23:30:00-09:00", "2026-08-12T08:30:00+00:00")]
    public async Task ActualWorkNormalizesOffsetBearingTimestampToUtc(string supplied, string expected)
    {
        var context = CreateContext();
        var activity = context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 12), context.Supervisor.Id);
        var repository = Repository(context.Tenant);
        var handler = new RecordActualWorkCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        await handler.Handle(new RecordActualWorkCommand(
            activity.Id, activity.Version, DateTimeOffset.Parse(supplied), 2m, null), CancellationToken.None);

        activity.ActualAt.ShouldBe(DateTimeOffset.Parse(expected));
        activity.ActualAt!.Value.Offset.ShouldBe(TimeSpan.Zero);
        activity.EntryDelayDays.ShouldBe(0);
        repository.Verify(store => store.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task InvalidEvidenceDoesNotMutateOrSave()
    {
        var context = CreateContext();
        var activity = context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), context.Supervisor.Id);
        var repository = Repository(context.Tenant);
        var handler = new AddSourceReferenceCommandHandler(
            repository.Object, User(), Identity(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => handler.Handle(
            new AddSourceReferenceCommand(
                activity.Id, activity.Version, "FS-204", new DateOnly(2026, 8, 13)),
            CancellationToken.None));
        activity.EvidenceLinks.ShouldBeEmpty();
        repository.Verify(store => store.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task FiltersAndPaginatesServerSideCollection()
    {
        var context = CreateContext();
        context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), context.Supervisor.Id);
        context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 14), context.Supervisor.Id);
        var handler = new GetActivitiesQueryHandler(Repository(context.Tenant).Object, User());

        var result = await handler.Handle(new GetActivitiesQuery(
            context.Field.Id, context.Cycle.Id, context.Type.Id, "Draft",
            new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 31), 2, 1), CancellationToken.None);

        result.TotalCount.ShouldBe(2);
        result.TotalPages.ShouldBe(2);
        result.Items.Count.ShouldBe(1);
        result.Page.ShouldBe(2);
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

    private static IIdentityService Identity()
    {
        var identity = new Mock<IIdentityService>();
        identity.Setup(service => service.GetUserNameAsync(It.IsAny<string>())).ReturnsAsync("grower@example.test");
        return identity.Object;
    }

    private static TestContext CreateContext()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var type = tenant.AddActivityType("SPRAY", "Foliar spray", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var supervisor = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(
            CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 1), new DateOnly(2027, 8, 31),
            900m, Now, "user-1");
        field.ActivateCropCycle(cycle, Now, "user-1");
        return new TestContext(tenant, farm, field, cycle, type, supervisor);
    }

    private sealed record TestContext(
        Tenant Tenant,
        Farm Farm,
        Field Field,
        CropCycle Cycle,
        ActivityType Type,
        Person Supervisor);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
