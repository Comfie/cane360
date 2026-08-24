using Cane360.Application.Activities;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/farm-personnel")]
public sealed class FarmPersonnelController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PersonnelRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PersonnelRegisterDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetPersonnelQuery(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<PersonnelRegisterDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PersonnelRegisterDto>> Create(
        CreatePersonRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreatePersonCommand(
            request.DisplayName,
            request.Phone,
            request.ActiveFrom,
            request.Roles,
            request.IsPrimaryManager), cancellationToken);
        return CreatedAtAction(nameof(Get), new { }, result);
    }

    [HttpPut("{personId:guid}")]
    [ProducesResponseType<PersonnelRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonnelRegisterDto>> Update(
        Guid personId,
        UpdatePersonRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdatePersonCommand(
            personId,
            request.DisplayName,
            request.Phone,
            request.Role,
            request.IsPrimaryManager,
            request.RoleEffectiveFrom,
            request.ExpectedVersion), cancellationToken));

    [HttpPost("{personId:guid}/deactivate")]
    [ProducesResponseType<PersonnelRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonnelRegisterDto>> Deactivate(
        Guid personId,
        DeactivatePersonRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new DeactivatePersonCommand(
            personId, request.ExpectedVersion, request.ActiveTo), cancellationToken));

    [HttpPost("{personId:guid}/roles/{assignmentId:guid}/end")]
    [ProducesResponseType<PersonnelRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonnelRegisterDto>> EndRole(
        Guid personId,
        Guid assignmentId,
        EndPersonRoleRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new EndPersonRoleCommand(
            personId, assignmentId, request.ExpectedVersion, request.EffectiveTo), cancellationToken));
}
