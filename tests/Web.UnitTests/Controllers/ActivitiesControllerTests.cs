using Cane360.Application.Activities;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public class ActivitiesControllerTests
{
    [Test]
    public void ControllerRequiresAuthentication()
    {
        typeof(ActivitiesController).GetCustomAttributes(typeof(AuthorizeAttribute), true)
            .ShouldNotBeEmpty();
    }

    [Test]
    public async Task GetMapsFiltersAndPagination()
    {
        var sender = new Mock<ISender>();
        var fieldId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var fromDate = new DateOnly(2026, 8, 1);
        var toDate = new DateOnly(2026, 8, 31);
        var expected = new ActivityCollectionDto([], 2, 20, 0, 0);
        sender.Setup(service => service.Send(
                It.Is<GetActivitiesQuery>(query =>
                    query.FieldId == fieldId &&
                    query.CropCycleId == cycleId &&
                    query.ActivityTypeId == typeId &&
                    query.Status == "Planned" &&
                    query.FromDate == fromDate &&
                    query.ToDate == toDate &&
                    query.Page == 2 &&
                    query.PageSize == 20),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ActivitiesController(sender.Object);

        var result = await controller.Get(
            fieldId, cycleId, typeId, "Planned", fromDate, toDate, 2, 20, CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task CreateMapsRequestAndReturnsActivityLocation()
    {
        var sender = new Mock<ISender>();
        var fieldId = Guid.NewGuid();
        var cycleId = Guid.NewGuid();
        var typeId = Guid.NewGuid();
        var supervisorId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var plannedDate = new DateOnly(2026, 8, 20);
        var expected = CreateDetails(activityId, fieldId, cycleId, typeId);
        sender.Setup(service => service.Send(
                It.Is<CreateActivityCommand>(command =>
                    command.FieldId == fieldId &&
                    command.CropCycleId == cycleId &&
                    command.ActivityTypeId == typeId &&
                    command.Kind == "Planned" &&
                    command.PlannedDate == plannedDate &&
                    command.SupervisorPersonId == supervisorId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ActivitiesController(sender.Object);

        var result = await controller.Create(
            new CreateActivityRequest(fieldId, cycleId, typeId, "Planned", plannedDate, supervisorId),
            CancellationToken.None);

        var created = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        created.RouteValues!["activityId"].ShouldBe(activityId);
        created.Value.ShouldBeSameAs(expected);
    }

    [Test]
    public async Task SourceReferenceMapsVersionAndMetadataSeparately()
    {
        var sender = new Mock<ISender>();
        var activityId = Guid.NewGuid();
        var capturedDate = new DateOnly(2026, 8, 12);
        var expected = CreateDetails(activityId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        sender.Setup(service => service.Send(
                It.Is<AddSourceReferenceCommand>(command =>
                    command.ActivityId == activityId &&
                    command.ExpectedVersion == 4 &&
                    command.SourceSheetReference == "FS-204" &&
                    command.CapturedDate == capturedDate),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ActivitiesController(sender.Object);

        var result = await controller.AddSourceReference(
            activityId, new AddSourceReferenceRequest(4, "FS-204", capturedDate), CancellationToken.None);

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    [TestCase("Planned")]
    [TestCase("Cancelled")]
    [TestCase("InProgress")]
    [TestCase("AwaitingVerification")]
    [TestCase("ManagerConfirmation")]
    [TestCase("Completed")]
    [TestCase("Closed")]
    public async Task ExplicitTransitionRoutesMapTargetAndVersion(string targetStatus)
    {
        var sender = new Mock<ISender>();
        var activityId = Guid.NewGuid();
        var expected = CreateDetails(activityId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        sender.Setup(service => service.Send(
                It.Is<TransitionActivityCommand>(command =>
                    command.ActivityId == activityId &&
                    command.TargetStatus == targetStatus &&
                    command.ExpectedVersion == 7 &&
                    command.Reason == "reason"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);
        var controller = new ActivitiesController(sender.Object);
        var request = new TransitionActivityRequest(7, "reason");

        ActionResult<ActivityDetailsDto> result = targetStatus switch
        {
            "Planned" => await controller.Planned(activityId, request, CancellationToken.None),
            "Cancelled" => await controller.Cancelled(activityId, request, CancellationToken.None),
            "InProgress" => await controller.InProgress(activityId, request, CancellationToken.None),
            "AwaitingVerification" => await controller.AwaitingVerification(activityId, request, CancellationToken.None),
            "ManagerConfirmation" => await controller.ManagerConfirmation(activityId, request, CancellationToken.None),
            "Completed" => await controller.Completed(activityId, request, CancellationToken.None),
            "Closed" => await controller.Closed(activityId, request, CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(targetStatus))
        };

        result.Result.ShouldBeOfType<OkObjectResult>().Value.ShouldBeSameAs(expected);
    }

    private static ActivityDetailsDto CreateDetails(
        Guid activityId,
        Guid fieldId,
        Guid cycleId,
        Guid activityTypeId) => new(
        new ActivityListItemDto(
            activityId,
            fieldId,
            "A-01",
            "North block",
            cycleId,
            activityTypeId,
            "WEED",
            "Weeding",
            "Planned",
            "2026-08-20",
            "Supervisor",
            "Hectares",
            null,
            null,
            false,
            false,
            0,
            null,
            "Draft",
            0,
            0),
        ["Planned"],
        new Dictionary<string, string>(),
        [],
        []);
}
