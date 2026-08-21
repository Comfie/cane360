using Cane360.Application.Common.Interfaces;
using Cane360.Application.CropCycles;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.CropCycles;

public class CropCycleLabourHistoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly WorkDate = new(2026, 8, 18);

    [Test]
    public async Task OneRecordAndOneActivityProducesOneLabourEvent()
    {
        var context = CreateContext();
        var record = CreateDailyRecord(context, [context.FirstActivity.Id]);

        var result = await Query(context, [record]);

        var labour = result.Timeline.Where(item => item.Type == "LabourEvidence").ToArray();
        labour.Length.ShouldBe(1);
        labour[0].Id.ShouldBe(record.Id);
        labour[0].Title.ShouldBe("Labour evidence · Weeding");
    }

    [Test]
    public async Task OneRecordAndMultipleActivitiesProducesOneEventWithEveryActivityName()
    {
        var context = CreateContext();
        var record = CreateDailyRecord(context, [context.SecondActivity.Id, context.FirstActivity.Id]);
        record.RecordSupervisorVerification(Guid.NewGuid(), Now.AddMinutes(1), "user-1", 0);
        record.Confirm(Now.AddMinutes(2), "user-1", 1);

        var result = await Query(context, [record, record]);

        record.CalculatedAmountUsd.ShouldBe(12m);
        var labour = result.Timeline.Where(item => item.Type == "LabourEvidence").ToArray();
        labour.Length.ShouldBe(1);
        labour[0].Id.ShouldBe(record.Id);
        labour[0].Title.ShouldBe("Labour evidence · Fertilising, Weeding");
    }

    [Test]
    public async Task MultipleRecordsRemainDistinctAndDeterministicallyOrdered()
    {
        var context = CreateContext();
        var first = CreateDailyRecord(context, [context.FirstActivity.Id], Now.AddMinutes(-2));
        var second = CreateDailyRecord(context, [context.FirstActivity.Id, context.SecondActivity.Id], Now.AddMinutes(-1));

        var result = await Query(context, [first, second]);

        var labour = result.Timeline.Where(item => item.Type == "LabourEvidence").ToArray();
        labour.Select(item => item.Id).ShouldBe([second.Id, first.Id]);
        labour.Select(item => item.Id).Distinct().Count().ShouldBe(2);
    }

    private static async Task<CropCycleDetailsDto> Query(TestContext context, IReadOnlyList<WorkRecord> records)
    {
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(repository => repository.GetTenantForUserAsync(
            "user-1", false, It.IsAny<CancellationToken>())).ReturnsAsync(context.Tenant);
        var labourRepository = new Mock<ILabourRepository>();
        labourRepository.Setup(repository => repository.GetWorkRecordsAsync(
            context.Tenant.Id, context.Farm.Id, null, null, null, false, It.IsAny<CancellationToken>())).ReturnsAsync(records);
        labourRepository.Setup(repository => repository.GetWorkersAsync(
            context.Tenant.Id, context.Farm.Id, false, It.IsAny<CancellationToken>())).ReturnsAsync([context.Worker]);
        var user = new Mock<IUser>();
        user.Setup(current => current.Id).Returns("user-1");
        var identity = new Mock<IIdentityService>();
        identity.Setup(service => service.GetUserNameAsync(It.IsAny<string>())).ReturnsAsync("manager@example.test");
        var handler = new GetCropCycleDetailsQueryHandler(
            farmRepository.Object, labourRepository.Object, user.Object, identity.Object);

        return await handler.Handle(
            new GetCropCycleDetailsQuery(context.Field.Id, context.Cycle.Id), CancellationToken.None);
    }

    private static WorkRecord CreateDailyRecord(
        TestContext context,
        IReadOnlyCollection<Guid> activityIds,
        DateTimeOffset? enteredAt = null) => WorkRecord.Create(
        context.Tenant.Id,
        context.Farm.Id,
        Guid.NewGuid(),
        context.Worker.Id,
        context.Field.Id,
        WorkDate,
        context.Rate,
        null,
        activityIds,
        enteredAt ?? Now,
        "user-1",
        null,
        0);

    private static TestContext CreateContext()
    {
        var tenant = Tenant.CreateForGrower("user-1", "Grower", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var weeding = tenant.AddActivityType("WEED", "Weeding", true, true, ActivityQuantityBasis.None);
        var fertilising = tenant.AddActivityType("FERT", "Fertilising", true, true, ActivityQuantityBasis.None);
        var farm = tenant.CreateFarm("GREEN", "Green Valley", "Plot 4", "Triangle", "Lease", 100m, "Furrow");
        var workerPerson = farm.AddPerson("Worker One", null, new DateOnly(2026, 1, 1));
        var supervisor = farm.AddPerson("Supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A-01", "North", 10m, null, ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31), 800m, Now, "user-1");
        field.ActivateCropCycle(cycle, Now, "user-1");
        var firstActivity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, weeding,
            ActivityPlanningKind.Planned, WorkDate, supervisor.Id);
        var secondActivity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, fertilising,
            ActivityPlanningKind.Planned, WorkDate, supervisor.Id);
        var worker = WorkerProfile.Create(Guid.NewGuid(), tenant.Id, farm.Id, workerPerson.Id,
            EmploymentType.Seasonal, new DateOnly(2026, 1, 1), [1], new byte[12], new byte[16],
            "test-v1", new byte[32], "••••••12");
        var rate = WorkerRate.Create(tenant.Id, farm.Id, worker.Id, PayBasis.Daily, null, 12m,
            new DateOnly(2026, 1, 1), null);
        return new TestContext(tenant, farm, field, cycle, firstActivity, secondActivity, worker, rate);
    }

    private sealed record TestContext(
        Tenant Tenant,
        Farm Farm,
        Field Field,
        CropCycle Cycle,
        Activity FirstActivity,
        Activity SecondActivity,
        WorkerProfile Worker,
        WorkerRate Rate);
}
