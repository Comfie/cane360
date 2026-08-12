using Cane360.Application.Activities;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/fields/{fieldId:guid}/line-profile")]
public sealed class FieldLineProfilesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FieldLineProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FieldLineProfileDto?>> Get(
        Guid fieldId,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetFieldLineProfileQuery(fieldId), cancellationToken);
        return result is null ? NoContent() : Ok(result);
    }

    [HttpPut]
    [ProducesResponseType<FieldLineProfileDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FieldLineProfileDto>> Replace(
        Guid fieldId,
        ReplaceFieldLineProfileRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ReplaceFieldLineProfileCommand(
            fieldId,
            request.StandardLineLengthMetres,
            request.EstimatedLineCount,
            request.NumberingScheme,
            request.EffectiveFrom,
            request.ExpectedVersion), cancellationToken));
}
