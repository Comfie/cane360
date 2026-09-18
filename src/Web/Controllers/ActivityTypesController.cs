using Cane360.Application.Activities;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/activity-types")]
public sealed class ActivityTypesController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetActivityTypes")]
    [ProducesResponseType<IReadOnlyList<ActivityTypeDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<ActivityTypeDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetActivityTypesQuery(), cancellationToken));
    }

    [HttpPost(Name = "CreateActivityTypes")]
    [ProducesResponseType<ActivityTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ActivityTypeDto>> Create(
        CreateActivityTypeRequest request,
        CancellationToken cancellationToken)
    {
        ActivityTypeDto result = await sender.Send(new CreateActivityTypeCommand(
            request.Code,
            request.Name,
            request.SupportsPlanned,
            request.SupportsUnplanned,
            request.QuantityBasis), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{activityTypeId:guid}/archive", Name = "ArchiveActivityTypes")]
    [ProducesResponseType<ActivityTypeDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivityTypeDto>> Archive(
        Guid activityTypeId,
        VersionedRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new ArchiveActivityTypeCommand(
            activityTypeId, request.ExpectedVersion), cancellationToken));
    }

    [HttpPut("{activityTypeId:guid}")]
    public async Task<ActionResult<ActivityTypeDto>> Rename(Guid activityTypeId,
        RenameActivityTypeRequest request, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new RenameActivityTypeCommand(activityTypeId,
            request.Name, request.ExpectedVersion), cancellationToken));
    }
}
