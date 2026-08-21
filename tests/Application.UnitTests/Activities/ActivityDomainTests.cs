using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Activities;

public class ActivityDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 12, 9, 30, 0, TimeSpan.Zero);

    [Test]
    public void ActivityTypeRequiresAtLeastOnePlanningMode()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        Should.Throw<InvalidOperationException>(() =>
            tenant.AddActivityType("RIP", "Ripping", false, false, ActivityQuantityBasis.Hectares));
    }

    [Test]
    public void ActiveAndReadyCyclesAcceptActivitiesButDraftDoesNot()
    {
        var context = CreateContext(ActivityQuantityBasis.None);
        Should.Throw<InvalidOperationException>(() => context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), context.Supervisor.Id));

        context.Field.ActivateCropCycle(context.Cycle, Now, "user-1");
        context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 13), context.Supervisor.Id).ShouldNotBeNull();
        context.Cycle.MarkReadyForHarvest(Now, "user-1");
        context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 14), context.Supervisor.Id).ShouldNotBeNull();
    }

    [Test]
    public void HectaresCannotExceedReportingArea()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.Hectares, ActivityPlanningKind.Planned);
        Should.Throw<InvalidOperationException>(() => context.Activity.RecordActualWork(
            Now.AddHours(-1), 12.5001m, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, Now, "user-1", null, 0));
    }

    [Test]
    public void StandardLinesMustBeWholeAndRespectKnownLineCount()
    {
        var context = CreateContext(ActivityQuantityBasis.StandardLines);
        context.Field.ReplaceLineProfile(100m, 80, "North to south", new DateOnly(2026, 8, 1));
        context.Field.ActivateCropCycle(context.Cycle, Now, "user-1");
        var activity = context.Cycle.CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Field.Id, context.Type,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 12), context.Supervisor.Id);

        Should.Throw<InvalidOperationException>(() => activity.RecordActualWork(
            Now.AddHours(-1), 2.5m, context.Field.ReportingHectares, context.Field.CurrentLineProfile,
            context.Cycle.StartDate, Now, "user-1", null, 0));
        Should.Throw<InvalidOperationException>(() => activity.RecordActualWork(
            Now.AddHours(-1), 81m, context.Field.ReportingHectares, context.Field.CurrentLineProfile,
            context.Cycle.StartDate, Now, "user-1", null, 0));
    }

    [Test]
    public void StandardLinesWithoutProfileArePermanentlyFlagged()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.StandardLines, ActivityPlanningKind.Planned);
        context.Activity.RecordActualWork(
            Now.AddHours(-1), 10m, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, Now, "system-user", null, 0);

        context.Activity.LineContextUnavailable.ShouldBeTrue();
        context.Activity.FieldLineProfileId.ShouldBeNull();
        context.Activity.ActualEnteredByUserId.ShouldBe("system-user");
        context.Activity.SupervisorPersonId.ShouldBe(context.Supervisor.Id);
    }

    [Test]
    public void HarareCalendarDelayOverTwoDaysRequiresReason()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.None, ActivityPlanningKind.Planned);
        var actualAt = new DateTimeOffset(2026, 8, 9, 21, 30, 0, TimeSpan.Zero); // 23:30 Harare
        var enteredAt = new DateTimeOffset(2026, 8, 12, 0, 30, 0, TimeSpan.Zero); // 02:30 Harare

        Should.Throw<InvalidOperationException>(() => context.Activity.RecordActualWork(
            actualAt, null, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, enteredAt, "user-1", null, 0));

        context.Activity.RecordActualWork(
            actualAt, null, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, enteredAt, "user-1", "Paper sheet arrived late", 0);
        context.Activity.EntryDelayDays.ShouldBe(3);
        context.Activity.LateEntryReason.ShouldBe("Paper sheet arrived late");
    }

    [Test]
    public void UnplannedWorkNeedsActualBeforePlannedAndLifecycleIsStrict()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.None, ActivityPlanningKind.Unplanned);
        Should.Throw<InvalidOperationException>(() => context.Activity.Transition(
            ActivityStatus.Planned, Now, "user-1", null, null, 0));
        context.Activity.RecordActualWork(
            Now.AddHours(-1), null, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, Now, "user-1", null, 0);
        context.Activity.Transition(ActivityStatus.Planned, Now, "user-1", null, null, 1);
        context.Activity.Transition(ActivityStatus.InProgress, Now, "user-1", null, null, 2);
        context.Activity.Transition(ActivityStatus.AwaitingVerification, Now, "user-1", null, null, 3);
        context.Activity.Transition(ActivityStatus.ManagerConfirmation, Now, "user-1", context.Supervisor.Id, null, 4);
        context.Activity.Transition(ActivityStatus.Completed, Now, "user-1", null, null, 5);
        context.Activity.Transition(ActivityStatus.Closed, Now, "user-1", null, null, 6);

        context.Activity.Status.ShouldBe(ActivityStatus.Closed);
        Should.Throw<InvalidOperationException>(() => context.Activity.RecordActualWork(
            Now, null, context.Field.ReportingHectares, null, context.Cycle.StartDate, Now, "user-1", null, 7));
        Should.Throw<InvalidOperationException>(() => context.Activity.AddSourceReference(
            "FS-1", new DateOnly(2026, 8, 12), Now, "user-1", 7));
    }

    [Test]
    public void CancellationNeedsReasonAndIsTerminal()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.None, ActivityPlanningKind.Planned);
        Should.Throw<InvalidOperationException>(() => context.Activity.Transition(
            ActivityStatus.Cancelled, Now, "user-1", null, "", 0));
        context.Activity.Transition(ActivityStatus.Cancelled, Now, "user-1", null, "Weather damage", 0);
        Should.Throw<InvalidOperationException>(() => context.Activity.Transition(
            ActivityStatus.Planned, Now, "user-1", null, null, 1));
    }

    [Test]
    public void CloseIsBlockedWhileRecordedLabourRemainsUnverified()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.None, ActivityPlanningKind.Planned);
        context.Activity.RecordActualWork(
            Now.AddHours(-1), null, context.Field.ReportingHectares, null,
            context.Cycle.StartDate, Now, "user-1", null, 0);
        context.Activity.Transition(ActivityStatus.Planned, Now, "user-1", null, null, 1);
        context.Activity.Transition(ActivityStatus.InProgress, Now, "user-1", null, null, 2);
        context.Activity.Transition(ActivityStatus.AwaitingVerification, Now, "user-1", null, null, 3);
        context.Activity.Transition(ActivityStatus.ManagerConfirmation, Now, "user-1", context.Supervisor.Id, null, 4);
        context.Activity.Transition(ActivityStatus.Completed, Now, "user-1", null, null, 5);

        Should.Throw<InvalidOperationException>(() => context.Activity.Transition(
            ActivityStatus.Closed, Now, "user-1", null, null, 6,
            allRequiredLabourVerified: false)).Message.ShouldContain("labour remains unverified");
        context.Activity.Status.ShouldBe(ActivityStatus.Completed);
    }

    [Test]
    public void HarvestIsBlockedUntilEveryActivityIsTerminal()
    {
        var context = CreateActiveActivity(ActivityQuantityBasis.None, ActivityPlanningKind.Planned);
        context.Cycle.MarkReadyForHarvest(Now, "user-1");
        Should.Throw<InvalidOperationException>(() => context.Cycle.RecordHarvest(
            new DateOnly(2026, 8, 12), 100m, new DateOnly(2026, 8, 12), Now, "user-1"));
        context.Activity.Transition(ActivityStatus.Cancelled, Now, "user-1", null, "No longer required", 0);
        context.Cycle.RecordHarvest(
            new DateOnly(2026, 8, 12), 100m, new DateOnly(2026, 8, 12), Now, "user-1");
        context.Cycle.Status.ShouldBe(CropCycleStatus.Harvested);
    }

    private static ActivityContext CreateActiveActivity(ActivityQuantityBasis basis, ActivityPlanningKind kind)
    {
        var context = CreateContext(basis);
        context.Field.ActivateCropCycle(context.Cycle, Now, "user-1");
        var activity = context.Cycle.CreateActivity(
            context.Tenant.Id,
            context.Farm.Id,
            context.Field.Id,
            context.Type,
            kind,
            kind == ActivityPlanningKind.Planned ? new DateOnly(2026, 8, 12) : null,
            context.Supervisor.Id);
        return context with { Activity = activity };
    }

    private static ActivityContext CreateContext(ActivityQuantityBasis basis)
    {
        var tenant = Tenant.CreateForGrower("user-1", "Tariro Moyo", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var type = tenant.AddActivityType("WORK", "Field work", true, true, basis);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 120m, "Furrow");
        var supervisor = farm.AddPerson("Rudo Ncube", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A-01", "North block", 12.5m, null, ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(
            CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 8, 1), new DateOnly(2027, 7, 1), new DateOnly(2027, 8, 31),
            900m, Now, "user-1");
        return new ActivityContext(tenant, farm, field, cycle, type, supervisor, null!);
    }

    private sealed record ActivityContext(
        Tenant Tenant,
        Farm Farm,
        Field Field,
        CropCycle Cycle,
        ActivityType Type,
        Person Supervisor,
        Activity Activity);
}
