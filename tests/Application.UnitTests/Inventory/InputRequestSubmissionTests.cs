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

public sealed class InputRequestSubmissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 22, 10, 0, 0, TimeSpan.Zero);

    [Test]
    public async Task MissingEffectiveRuleBlocksSubmissionWithActionableValidation()
    {
        var setup = CreateSetup();
        var farmRepository = new Mock<IFarmSetupRepository>();
        farmRepository.Setup(repository => repository.GetTenantForUserAsync(
            "grower-user", false, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Tenant);
        var inventory = new Mock<IInventoryRepository>();
        inventory.Setup(repository => repository.GetInputRequestAsync(setup.Tenant.Id,
            setup.Farm.Id, setup.Request.Id, true, It.IsAny<CancellationToken>())).ReturnsAsync(setup.Request);
        inventory.Setup(repository => repository.GetEffectiveRuleAsync(setup.Tenant.Id,
            setup.Farm.Id, setup.Line.InventoryItemId, setup.ActivityTypeId,
            setup.Request.OperationalDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryApplicationRule?)null);
        var user = new Mock<IUser>();
        user.Setup(value => value.Id).Returns("grower-user");
        var handler = new SubmitInputRequestCommandHandler(farmRepository.Object, inventory.Object,
            user.Object, new FixedTimeProvider(Now));

        var error = await Should.ThrowAsync<ValidationException>(() => handler.Handle(
            new SubmitInputRequestCommand(setup.Request.Id, setup.Request.Version, "missing-rule"),
            CancellationToken.None));

        error.Errors.SelectMany(entry => entry.Value)
            .ShouldContain(message => message.Contains("changed or is missing", StringComparison.Ordinal));
        setup.Request.Status.ShouldBe(InputRequestStatus.Draft);
        inventory.Verify(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SubmissionSetup CreateSetup()
    {
        var tenant = Tenant.CreateForGrower("grower-user", "Grower", null);
        var variety = tenant.AddCropVariety("N14", "N14");
        var activityType = tenant.AddActivityType(
            "FERT", "Fertilising", true, true, ActivityQuantityBasis.Hectares);
        var farm = tenant.CreateFarm("FARM", "Farm", "Address", "Location", "Lease", 20m, "Furrow");
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
        var line = request.AddLine(item, rule, 10m, 100m, 200m, 3m, request.Version);
        return new(tenant, farm, request, line, activityType.Id);
    }

    private sealed record SubmissionSetup(
        Tenant Tenant, Farm Farm, InputRequest Request, InputRequestLine Line, Guid ActivityTypeId);

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
