using Cane360.Application.Inventory;
using Cane360.Domain.Inventory;
using Cane360.Web.Controllers;
using Cane360.Web.Models.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.UnitTests.Controllers;

public sealed class InputControlsControllerTests
{
    [Test]
    public void ControllerRequiresAuthenticatedMembership()
    {
        typeof(InputControlsController).GetCustomAttributes(typeof(AuthorizeAttribute), true).ShouldNotBeEmpty();
    }

    [Test]
    public async Task FieldReceiptPreservesOffsetTimestampAndTraceIdentifiers()
    {
        var sender = new Mock<ISender>();
        var issueId = Guid.NewGuid(); var fieldId = Guid.NewGuid(); var cycleId = Guid.NewGuid(); var activityId = Guid.NewGuid();
        var recipientId = Guid.NewGuid(); var lineId = Guid.NewGuid();
        var receivedAt = DateTimeOffset.Parse("2026-08-24T09:30:00+02:00");
        sender.Setup(value => value.Send(It.Is<CreateFieldReceiptCommand>(command =>
                command.StockIssueId == issueId && command.FieldId == fieldId && command.CropCycleId == cycleId &&
                command.ActivityId == activityId && command.RecipientPersonId == recipientId &&
                command.ReceivedAt == receivedAt && command.Lines.Single().StockIssueLineId == lineId),
            It.IsAny<CancellationToken>())).ReturnsAsync(Guid.NewGuid());
        var controller = new InputControlsController(sender.Object);

        await controller.CreateFieldReceipt(new CreateFieldReceiptRequest(issueId, fieldId, cycleId, activityId,
            recipientId, receivedAt, null, [new CreateFieldReceiptLineRequest(lineId, 2.5m)]), CancellationToken.None);

        sender.VerifyAll();
    }

    [Test]
    public async Task ApplicationAttestationAndManagerConfirmationDispatchDistinctCommandsWithExpectedVersions()
    {
        var sender = new Mock<ISender>();
        var applicationId = Guid.NewGuid(); var supervisorId = Guid.NewGuid();
        sender.Setup(value => value.Send(It.Is<AttestInputApplicationCommand>(command =>
                command.InputApplicationId == applicationId && command.SupervisorPersonId == supervisorId &&
                command.ExpectedVersion == 3), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        sender.Setup(value => value.Send(It.Is<ConfirmInputApplicationCommand>(command =>
                command.InputApplicationId == applicationId && command.ExpectedVersion == 4 &&
                command.LateConfirmationReason == "Delayed verification" && command.IdempotencyKey == "confirm-1"),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var controller = new InputControlsController(sender.Object);

        await controller.AttestApplication(applicationId, new AttestInputApplicationRequest(supervisorId, "Checked", 3), CancellationToken.None);
        await controller.ConfirmApplication(applicationId, new ConfirmInputApplicationRequest("Delayed verification", 4, "confirm-1"), CancellationToken.None);

        sender.VerifyAll();
    }

    [Test]
    public async Task LossDecisionPreservesExactVersionAndNeverAcceptsPayloadRole()
    {
        var sender = new Mock<ISender>();
        var lossId = Guid.NewGuid();
        sender.Setup(value => value.Send(It.Is<DecideInventoryLossCommand>(command => command.InventoryLossId == lossId &&
                command.ExpectedVersion == 7 && command.Outcome == ApprovalOutcome.Approved &&
                command.IdempotencyKey == "grower-decision"), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var controller = new InputControlsController(sender.Object);

        await controller.DecideLoss(lossId, new DecideInventoryLossRequest(7, ApprovalOutcome.Approved, null, "grower-decision"), CancellationToken.None);

        sender.VerifyAll();
        typeof(DecideInventoryLossRequest).GetProperties().Select(property => property.Name).ShouldNotContain("Role");
    }

    [Test]
    public async Task CorrectionRejectsUnsupportedOutcomeBeforeDispatch()
    {
        var sender = new Mock<ISender>();
        var controller = new InputControlsController(sender.Object);

        var result = await controller.DecideFieldAccountabilityCorrection(Guid.NewGuid(),
            new DecideFieldAccountabilityCorrectionRequest("NotAnOutcome", 1, null, "key"), CancellationToken.None);

        result.ShouldBeOfType<BadRequestObjectResult>();
        sender.Verify(value => value.Send(It.IsAny<IRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
