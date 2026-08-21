using Cane360.Application.Common.Interfaces;
using Cane360.Application.Labour;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Labour;

public class LabourCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly WorkDate = new(2026, 8, 18);

    [Test]
    public async Task DuplicateNationalIdIsRejectedBeforeDatabaseSave()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Grower", null);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 100m, "Furrow");
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(item => item.GetTenantForUserAsync("user-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        var fingerprint = Enumerable.Range(1, 32).Select(value => (byte)value).ToArray();
        var protector = new Mock<IWorkerSensitiveDataProtector>();
        protector.Setup(item => item.Protect(tenant.Id, farm.Id, It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns(new ProtectedNationalId([1], new byte[12], new byte[16], "test-v1", fingerprint, "••••••12"));
        var labour = new Mock<ILabourRepository>();
        labour.Setup(item => item.HasNationalIdFingerprintAsync(
                tenant.Id, farm.Id, fingerprint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateWorkerCommandHandler(
            farmRepository.Object, labour.Object, protector.Object, User(), new FixedTimeProvider(Now));

        var exception = await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ConflictException>(() => handler.Handle(
            new CreateWorkerCommand(null, "Worker One", null, "Seasonal", WorkDate, "63-123456-A-12"),
            CancellationToken.None));

        exception.Message.ShouldBe("A worker with this national ID is already registered on this farm.");
        labour.Verify(item => item.Add(It.IsAny<WorkerProfile>()), Times.Never);
        labour.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CreatePieceEvidenceUsesTheSingleEventDateRateSnapshot()
    {
        var context = CreateContext(AttendanceStatus.Present);
        var oldRate = WorkerRate.Create(context.Tenant.Id, context.Farm.Id, context.Worker.Id,
            PayBasis.Hectare, context.ActivityType.Id, 10m, new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31));
        var applicableRate = WorkerRate.Create(context.Tenant.Id, context.Farm.Id, context.Worker.Id,
            PayBasis.Hectare, context.ActivityType.Id, 24.50m, new DateOnly(2026, 8, 1), null);
        var labour = LabourRepository(context, [oldRate, applicableRate]);
        WorkRecord? added = null;
        labour.Setup(repository => repository.Add(It.IsAny<WorkRecord>()))
            .Callback<WorkRecord>(record => added = record);
        var handler = new CreateWorkRecordCommandHandler(
            FarmRepository(context.Tenant).Object, labour.Object, User(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateWorkRecordCommand(
            context.Worker.Id, WorkDate, "Hectare", [context.Activity.Id], 2m,
            new WorkScopeCommand("NamedSection", null, null, "North ridge"), null), CancellationToken.None);

        result.AppliedRateUsd.ShouldBe(24.50m);
        result.Quantity.ShouldBe(2m);
        added.ShouldNotBeNull();
        added.WorkerRateId.ShouldBe(applicableRate.Id);
        added.RateEffectiveFrom.ShouldBe(new DateOnly(2026, 8, 1));
        labour.Verify(repository => repository.SaveChangesAsync(CancellationToken.None), Times.Once);
    }

    [Test]
    public async Task DailyEvidenceCanLinkMultipleActivitiesOnTheAllocatedFieldAndDate()
    {
        var context = CreateContext(AttendanceStatus.Present);
        var secondActivity = context.Farm.Fields.SelectMany(field => field.CropCycles).Single().CreateActivity(
            context.Tenant.Id, context.Farm.Id, context.Activity.FieldId, context.ActivityType,
            ActivityPlanningKind.Planned, WorkDate, context.Activity.SupervisorPersonId);
        secondActivity.RecordActualWork(Now.AddMinutes(-30), 4m,
            context.Farm.Fields.Single(field => field.Id == context.Activity.FieldId).ReportingHectares,
            null, new DateOnly(2026, 1, 1), Now, "user-1", null, 0);
        var rate = WorkerRate.Create(context.Tenant.Id, context.Farm.Id, context.Worker.Id,
            PayBasis.Daily, null, 12m, new DateOnly(2026, 8, 1), null);
        var labour = LabourRepository(context, [rate]);
        WorkRecord? added = null;
        labour.Setup(repository => repository.Add(It.IsAny<WorkRecord>()))
            .Callback<WorkRecord>(record => added = record);
        var handler = new CreateWorkRecordCommandHandler(
            FarmRepository(context.Tenant).Object, labour.Object, User(), new FixedTimeProvider(Now));

        var result = await handler.Handle(new CreateWorkRecordCommand(
            context.Worker.Id, WorkDate, "Daily", [context.Activity.Id, secondActivity.Id], null,
            null, null), CancellationToken.None);

        added.ShouldNotBeNull();
        added.Activities.Select(link => link.ActivityId).ShouldBe([context.Activity.Id, secondActivity.Id], ignoreOrder: true);
        result.ActivityIds.Count.ShouldBe(2);
    }

    [Test]
    public async Task AbsentWorkerCannotReceivePaidWork()
    {
        var context = CreateContext(AttendanceStatus.Absent);
        var rate = WorkerRate.Create(context.Tenant.Id, context.Farm.Id, context.Worker.Id,
            PayBasis.Hectare, context.ActivityType.Id, 24.50m, new DateOnly(2026, 8, 1), null);
        var labour = LabourRepository(context, [rate]);
        var handler = new CreateWorkRecordCommandHandler(
            FarmRepository(context.Tenant).Object, labour.Object, User(), new FixedTimeProvider(Now));

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => handler.Handle(
            new CreateWorkRecordCommand(context.Worker.Id, WorkDate, "Hectare", [context.Activity.Id], 2m,
                new WorkScopeCommand("NamedSection", null, null, "North ridge"), null), CancellationToken.None));

        labour.Verify(repository => repository.Add(It.IsAny<WorkRecord>()), Times.Never);
        labour.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CrossCycleAndCrossFieldActivityLinkIsRejected()
    {
        var context = CreateContext(AttendanceStatus.Present);
        var dailyRate = WorkerRate.Create(context.Tenant.Id, context.Farm.Id, context.Worker.Id,
            PayBasis.Daily, null, 12m, new DateOnly(2026, 8, 1), null);
        var labour = LabourRepository(context, [dailyRate]);
        var handler = new CreateWorkRecordCommandHandler(
            FarmRepository(context.Tenant).Object, labour.Object, User(), new FixedTimeProvider(Now));
        var variety = context.Tenant.CropVarieties.Single();
        var supervisor = context.Farm.Persons.Single(person => person.DisplayName == "Supervisor");
        var otherField = context.Farm.AddField(
            "B-01", "South", 5m, null, ReportingAreaSource.Declared, "Furrow", null);
        var otherFieldCycle = otherField.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31), 400m, Now, "user-1");
        otherField.ActivateCropCycle(otherFieldCycle, Now, "user-1");
        var otherFieldActivity = otherFieldCycle.CreateActivity(context.Tenant.Id, context.Farm.Id, otherField.Id,
            context.ActivityType, ActivityPlanningKind.Planned, WorkDate, supervisor.Id);
        otherFieldActivity.RecordActualWork(Now.AddHours(-1), 2m, otherField.ReportingHectares, null,
            otherFieldCycle.StartDate, Now, "user-1", null, 0);

        await Should.ThrowAsync<Cane360.Application.Common.Exceptions.ValidationException>(() => handler.Handle(
            new CreateWorkRecordCommand(context.Worker.Id, WorkDate, "Daily", [otherFieldActivity.Id], null,
                null, null), CancellationToken.None));

        labour.Verify(repository => repository.Add(It.IsAny<WorkRecord>()), Times.Never);
    }

    private static TestLabourContext CreateContext(AttendanceStatus attendanceStatus)
    {
        var tenant = Tenant.CreateForGrower("user-1", "Grower", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var activityType = tenant.AddActivityType("WEED", "Weeding", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 100m, "Furrow");
        var person = farm.AddPerson("Worker One", null, new DateOnly(2026, 1, 1));
        var supervisor = farm.AddPerson("Supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A-01", "North", 10m, null, ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31), 800m, Now, "user-1");
        field.ActivateCropCycle(cycle, Now, "user-1");
        var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, activityType,
            ActivityPlanningKind.Planned, WorkDate, supervisor.Id);
        activity.RecordActualWork(Now.AddHours(-1), 5m, field.ReportingHectares, null,
            cycle.StartDate, Now, "user-1", null, 0);
        var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, person.Id, EmploymentType.Seasonal,
            new DateOnly(2026, 1, 1), [1], new byte[12], new byte[16], "test-v1", new byte[32], "••••••12");
        var attendance = Attendance.Create(tenant.Id, farm.Id, worker.Id, WorkDate, attendanceStatus,
            attendanceStatus == AttendanceStatus.Present ? field.Id : null, Now, "user-1", null, 0);
        return new TestLabourContext(tenant, farm, activityType, activity, worker, attendance);
    }

    private static Mock<IFarmSetupRepository> FarmRepository(Tenant tenant)
    {
        var repository = new Mock<IFarmSetupRepository>();
        repository.Setup(item => item.GetTenantForUserAsync("user-1", false, It.IsAny<CancellationToken>())).ReturnsAsync(tenant);
        return repository;
    }

    private static Mock<ILabourRepository> LabourRepository(TestLabourContext context, IReadOnlyList<WorkerRate> rates)
    {
        var repository = new Mock<ILabourRepository>();
        repository.Setup(item => item.GetWorkerAsync(context.Tenant.Id, context.Farm.Id, context.Worker.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(context.Worker);
        repository.Setup(item => item.GetAttendanceAsync(context.Tenant.Id, context.Farm.Id, context.Worker.Id, WorkDate, false, It.IsAny<CancellationToken>())).ReturnsAsync(context.Attendance);
        repository.Setup(item => item.GetRatesAsync(context.Tenant.Id, context.Farm.Id, context.Worker.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync(rates);
        repository.Setup(item => item.GetWorkRecordsAsync(context.Tenant.Id, context.Farm.Id, null, null, context.Activity.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repository.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return repository;
    }

    private static IUser User()
    {
        var user = new Mock<IUser>();
        user.Setup(item => item.Id).Returns("user-1");
        user.Setup(item => item.CorrelationId).Returns("test-correlation");
        return user.Object;
    }

    private sealed record TestLabourContext(Tenant Tenant, Farm Farm, ActivityType ActivityType,
        Activity Activity, WorkerProfile Worker, Attendance Attendance);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
