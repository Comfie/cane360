using Cane360.Application.Inventory;
using Cane360.Web.Models.Inventory;
using Cane360.Domain.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/input-controls")]
public sealed class InputControlsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<InputControlWorkspaceDto>> Workspace(
        [FromQuery] Guid? activityId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetInputControlWorkspaceQuery(activityId), cancellationToken));

    [HttpPost("rules")]
    public async Task<ActionResult<InventoryApplicationRuleDto>> CreateRule(
        CreateInventoryApplicationRuleRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateInventoryApplicationRuleCommand(
            request.InventoryItemId, request.ActivityTypeId, request.EffectiveFrom, request.EffectiveTo,
            request.CoverageBasis, request.RatePerCoverageUnit,
            request.LowerTolerancePercent, request.UpperTolerancePercent), cancellationToken));

    [HttpPost("requests")]
    public async Task<ActionResult<Guid>> CreateRequest(
        CreateInputRequestRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateInputRequestCommand(
            request.ActivityId, request.Lines.Select(line => new CreateInputRequestLineCommand(
                line.InventoryItemId, line.RequestedQuantity)).ToArray()), cancellationToken));

    [HttpPut("requests/{requestId:guid}/lines/{lineId:guid}")]
    public async Task<IActionResult> EditRequestLine(Guid requestId, Guid lineId,
        EditInputRequestLineRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new EditInputRequestLineCommand(
            requestId, lineId, request.RequestedQuantity, request.ExpectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/submit")]
    public async Task<IActionResult> SubmitRequest(Guid requestId,
        PostStockReceiptRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitInputRequestCommand(
            requestId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/decision")]
    public async Task<IActionResult> DecideRequest(Guid requestId,
        DecideInputRequestRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApprovalOutcome>(request.Outcome, true, out var outcome))
            return BadRequest("Outcome must be Approved or Rejected.");
        await sender.Send(new DecideInputRequestCommand(requestId, request.ExpectedVersion,
            outcome, request.Reason, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("requests/{requestId:guid}/cancel")]
    public async Task<IActionResult> CancelRequest(Guid requestId,
        CancelInputRequestRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new CancelInputRequestCommand(
            requestId, request.ExpectedVersion, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("issues")]
    public async Task<ActionResult<Guid>> CreateIssue(
        CreateStockIssueRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateStockIssueCommand(
            request.InputRequestId, request.IssueDate, request.IssuerPersonId,
            request.RecipientPersonId, request.LateEntryReason,
            request.Lines.Select(line => new CreateStockIssueLineCommand(
                line.InputRequestLineId, line.InventoryLotId, line.Quantity)).ToArray()), cancellationToken));

    [HttpPost("issues/{issueId:guid}/post")]
    public async Task<IActionResult> PostIssue(Guid issueId,
        PostStockReceiptRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new PostStockIssueCommand(
            issueId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("issues/{issueId:guid}/correction")]
    public async Task<IActionResult> RequestCorrection(Guid issueId,
        RequestStockIssueCorrectionRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RequestStockIssueCorrectionCommand(
            issueId, request.ExpectedVersion, request.Reason), cancellationToken);
        return NoContent();
    }

    [HttpPost("issues/{issueId:guid}/reverse")]
    public async Task<IActionResult> ReverseIssue(Guid issueId,
        ReverseStockIssueRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ReverseStockIssueCommand(issueId, request.ExpectedVersion,
            request.Reason, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("field-receipts")]
    public async Task<ActionResult<Guid>> CreateFieldReceipt(CreateFieldReceiptRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateFieldReceiptCommand(request.StockIssueId,
            request.FieldId, request.CropCycleId, request.ActivityId, request.RecipientPersonId,
            request.ReceivedAt, request.LateEntryReason, request.Lines.Select(x =>
                new CreateFieldReceiptLineCommand(x.StockIssueLineId, x.Quantity)).ToArray()), cancellationToken));

    [HttpPost("applications")]
    public async Task<ActionResult<Guid>> CreateApplication(CreateInputApplicationRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateInputApplicationCommand(request.ActivityId,
            request.AppliedAt, request.CoverageBasis, request.VerifiedCoverage, request.Lines.Select(x =>
                new CreateInputApplicationLineCommand(x.FieldReceiptLineId, x.StockIssueLineId, x.AppliedQuantity)).ToArray()), cancellationToken));

    [HttpPost("applications/{applicationId:guid}/attestation")]
    public async Task<IActionResult> AttestApplication(Guid applicationId, AttestInputApplicationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new AttestInputApplicationCommand(applicationId, request.SupervisorPersonId, request.Note, request.ExpectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPost("applications/{applicationId:guid}/confirmation")]
    public async Task<IActionResult> ConfirmApplication(Guid applicationId, ConfirmInputApplicationRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ConfirmInputApplicationCommand(applicationId, request.LateConfirmationReason, request.ExpectedVersion, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("returns")]
    public async Task<ActionResult<Guid>> CreateReturn(CreateStockReturnRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateStockReturnCommand(request.ActivityId,
            request.ReturnDate, request.SenderPersonId, request.ReceiverPersonId, request.Lines.Select(x =>
                new CreateStockReturnLineCommand(x.StockIssueLineId, x.Quantity)).ToArray()), cancellationToken));

    [HttpPost("returns/{stockReturnId:guid}/post")]
    public async Task<IActionResult> PostReturn(Guid stockReturnId, PostStockReturnRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new PostStockReturnCommand(stockReturnId, request.ExpectedVersion, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("returns/{stockReturnId:guid}/reverse")]
    public async Task<IActionResult> ReverseReturn(Guid stockReturnId, ReverseStockReturnRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new ReverseStockReturnCommand(stockReturnId, request.ExpectedVersion,
            request.Reason, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("losses")]
    public async Task<ActionResult<Guid>> CreateLoss(CreateInventoryLossRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateInventoryLossCommand(request.ActivityId,
            request.StockIssueLineId, request.Quantity, request.LossType, request.Reason), cancellationToken));

    [HttpPost("losses/{lossId:guid}/submit")]
    public async Task<IActionResult> SubmitLoss(Guid lossId, VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new SubmitInventoryLossCommand(lossId, request.ExpectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPost("losses/{lossId:guid}/decision")]
    public async Task<IActionResult> DecideLoss(Guid lossId, DecideInventoryLossRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new DecideInventoryLossCommand(lossId, request.ExpectedVersion, request.Outcome,
            request.Reason, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("corrections")]
    public async Task<ActionResult<Guid>> CreateFieldAccountabilityCorrection(
        CreateFieldAccountabilityCorrectionRequest request, CancellationToken cancellationToken) =>
        CreatedAtAction(nameof(Workspace), await sender.Send(new CreateFieldAccountabilityCorrectionCommand(
            request.FieldReceiptId, request.InputApplicationId, request.StockReturnId, request.InventoryLossId,
            request.SourceVersion, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("corrections/{correctionId:guid}/decision")]
    public async Task<IActionResult> DecideFieldAccountabilityCorrection(Guid correctionId,
        DecideFieldAccountabilityCorrectionRequest request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<ApprovalOutcome>(request.Outcome, true, out var outcome))
            return BadRequest("Outcome must be Approved or Rejected.");
        await sender.Send(new DecideFieldAccountabilityCorrectionCommand(correctionId, request.ExpectedVersion,
            outcome, request.Reason, request.IdempotencyKey), cancellationToken);
        return NoContent();
    }

    [HttpPost("manager-invitations")]
    public async Task<ActionResult<CreatedManagerInvitationDto>> CreateInvitation(
        CreateManagerInvitationRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CreateManagerInvitationCommand(
            request.PersonId, request.ExpiresInHours), cancellationToken));

    [HttpPost("manager-invitations/{invitationId:guid}/revoke")]
    public async Task<IActionResult> RevokeInvitation(Guid invitationId,
        VersionedInventoryRequest request, CancellationToken cancellationToken)
    {
        await sender.Send(new RevokeManagerInvitationCommand(
            invitationId, request.ExpectedVersion), cancellationToken);
        return NoContent();
    }

    [HttpPost("manager-invitations/redeem")]
    public async Task<ActionResult<TenantSessionDto>> RedeemInvitation(
        RedeemManagerInvitationRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new RedeemManagerInvitationCommand(request.Token), cancellationToken));
}
