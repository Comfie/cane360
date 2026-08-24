using Cane360.Application.FarmSetup;
using Cane360.Web.Models.FarmSetup;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class FarmSetupController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Get farm setup")]
    [EndpointDescription("Returns the authenticated grower's farm, fields, and current crop cycles.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FarmSetupDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetFarmSetupQuery(), cancellationToken));

    [HttpPost("farm")]
    [EndpointSummary("Create grower farm")]
    [EndpointDescription("Creates the grower tenant, profile, active farm, membership, and default store.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FarmSetupDto>> CreateFarm(
        CreateGrowerFarmRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CreateGrowerFarmCommand(
            request.GrowerDisplayName,
            request.GrowerPhone,
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext), cancellationToken));

    [HttpPut("farm")]
    [EndpointSummary("Update grower farm")]
    [EndpointDescription("Updates the authenticated grower's profile and active farm details.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmSetupDto>> UpdateFarm(
        UpdateFarmInformationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new UpdateFarmInformationCommand(
            request.GrowerDisplayName,
            request.GrowerPhone,
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext), cancellationToken));

    [HttpPost("fields")]
    [EndpointSummary("Create field")]
    [EndpointDescription("Adds a uniquely coded field to the authenticated grower's active farm.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmSetupDto>> CreateField(
        CreateFieldRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CreateFieldCommand(
            request.Code,
            request.Name,
            request.DeclaredHectares,
            request.MappedHectares,
            request.ReportingAreaSource,
            request.IrrigationMethod,
            request.SoilNotes), cancellationToken));

}
