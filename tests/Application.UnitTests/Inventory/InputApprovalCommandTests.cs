using Cane360.Application.Common.Exceptions;
using Cane360.Application.Common.Interfaces;
using Cane360.Application.Inventory;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace Cane360.Application.UnitTests.Inventory;

public sealed class InputApprovalCommandTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task FarmManagerCannotApproveGrowerRequiredRequest()
    {
        var context = CreateContext(120m);
        var handler = Handler(context, "manager-user", out _);

        await Should.ThrowAsync<ForbiddenAccessException>(() => handler.Handle(
            new DecideInputRequestCommand(context.Request.Id, context.Request.Version,
                ApprovalOutcome.Approved, null, "manager-decision"), CancellationToken.None));
    }

    [Test]
    public async Task FarmManagerMayApproveRequestInsideInclusiveTolerance()
    {
        var context = CreateContext(100m);
        var handler = Handler(context, "manager-user", out var inventory);
        ApprovalDecision? decision = null;
        inventory.Setup(repository => repository.Add(It.IsAny<ApprovalDecision>()))
            .Callback<ApprovalDecision>(value => decision = value);

        await handler.Handle(new DecideInputRequestCommand(context.Request.Id, context.Request.Version,
            ApprovalOutcome.Approved, null, "manager-normal-decision"), CancellationToken.None);

        decision.ShouldNotBeNull();
        decision.ApproverRole.ShouldBe(TenantSecurityRoles.FarmManager);
        decision.SubjectVersion.ShouldBe(context.Request.Version - 1);
        context.Request.Status.ShouldBe(InputRequestStatus.Approved);
    }

    [Test]
    public async Task GrowerDecisionCreatesImmutableExactVersionApproval()
    {
        var context = CreateContext(120m);
        var handler = Handler(context, "grower-user", out var inventory);
        ApprovalDecision? decision = null;
        inventory.Setup(repository => repository.Add(It.IsAny<ApprovalDecision>()))
            .Callback<ApprovalDecision>(value => decision = value);

        await handler.Handle(new DecideInputRequestCommand(context.Request.Id, context.Request.Version,
            ApprovalOutcome.Approved, null, "grower-decision"), CancellationToken.None);

        decision.ShouldNotBeNull();
        decision.InputRequestId.ShouldBe(context.Request.Id);
        decision.SubjectVersion.ShouldBe(context.Request.Version - 1);
        decision.ApproverRole.ShouldBe(TenantSecurityRoles.Grower);
        context.Request.Status.ShouldBe(InputRequestStatus.Approved);
    }

    private static DecideInputRequestCommandHandler Handler(
        ApprovalContext context, string userId, out Mock<IInventoryRepository> inventory)
    {
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(repository => repository.GetTenantForUserAsync(
            userId, false, It.IsAny<CancellationToken>())).ReturnsAsync(context.Tenant);
        inventory = new Mock<IInventoryRepository>();
        inventory.Setup(repository => repository.GetInputRequestAsync(context.Tenant.Id,
            context.Farm.Id, context.Request.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(context.Request);
        inventory.Setup(repository => repository.GetInputRequestApprovalAsync(
            context.Request.Id, context.Request.Version, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApprovalDecision?)null);
        inventory.Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns(userId);
        user.Setup(value => value.CorrelationId).Returns("p5b-approval-test");
        return new DecideInputRequestCommandHandler(farmRepository.Object, inventory.Object,
            user.Object, new FixedTimeProvider(Now));
    }

    private static ApprovalContext CreateContext(decimal requestedQuantity)
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Grower", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var activityType = tenant.AddActivityType("FERT", "Fertilising", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Lease", 20m, "Furrow");
        var manager = farm.AddPerson("Manager", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(manager, PersonRole.FarmManager, true, new DateOnly(2026, 1, 1));
        tenant.AddFarmManagerMembership("manager-user", manager.Id);
        var supervisor = farm.AddPerson("Supervisor", null, new DateOnly(2026, 1, 1));
        farm.AssignRole(supervisor, PersonRole.Supervisor, false, new DateOnly(2026, 1, 1));
        var field = farm.AddField("A1", "Block A", 10m, null,
            ReportingAreaSource.Declared, "Furrow", null);
        var cycle = field.CreateCropCycleDraft(CropCycleType.PlantCane, null, variety, variety.Name,
            new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 1), new DateOnly(2027, 1, 31),
            800m, Now, "grower-user");
        field.ActivateCropCycle(cycle, Now, "grower-user");
        var activity = cycle.CreateActivity(tenant.Id, farm.Id, field.Id, activityType,
            ActivityPlanningKind.Planned, new DateOnly(2026, 8, 22), supervisor.Id);
        var unit = UnitOfMeasure.Create(tenant.Id, "KG", "Kilogram", "Mass", 3);
        var item = InventoryItem.Create(tenant.Id, farm.Id, "FERT-1", "Fertiliser",
            InventoryItemCategory.Fertiliser, unit, null, LotTrackingPolicy.None, ExpiryPolicy.None);
        var rule = InventoryApplicationRule.Create(tenant.Id, farm.Id, item, activityType.Id,
            new DateOnly(2026, 1, 1), null, ApplicationCoverageBasis.FieldReportingHectares,
            10m, 5m, 10m);
        var request = InputRequest.Create(tenant.Id, farm.Id, field.Id, cycle.Id, activity.Id,
            new DateOnly(2026, 8, 22), "grower-user");
        request.AddLine(item, rule, field.ReportingHectares, requestedQuantity, 200m, 2m, request.Version);
        request.Submit(Now, "submit", request.Version);
        request.OpenApproval(request.Version);
        return new(tenant, farm, request);
    }

    private sealed record ApprovalContext(Tenant Tenant, Farm Farm, InputRequest Request);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
