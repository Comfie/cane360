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
