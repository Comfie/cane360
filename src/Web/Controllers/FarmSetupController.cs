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
    public async Task<ActionResult<FarmSetupDto>> Get(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetFarmSetupQuery(), cancellationToken));
    }

    [HttpPost("farm", Name = "CreateFarmFarmSetup")]
    [EndpointSummary("Create grower farm")]
    [EndpointDescription("Creates the grower tenant, profile, active farm, membership, and default store.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FarmSetupDto>> CreateFarm(
        CreateGrowerFarmRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new CreateGrowerFarmCommand(
            request.GrowerDisplayName,
            request.GrowerPhone,
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext, request.OwnerProfile), cancellationToken));
    }

    [HttpPut("farm", Name = "UpdateFarmFarmSetup")]
    [EndpointSummary("Update grower farm")]
    [EndpointDescription("Updates the authenticated grower's profile and active farm details.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmSetupDto>> UpdateFarm(
        UpdateFarmInformationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new UpdateFarmInformationCommand(
            request.GrowerDisplayName,
            request.GrowerPhone,
            request.FarmCode,
            request.FarmName,
            request.Address,
            request.Location,
            request.Tenure,
            request.DeclaredHectares,
            request.IrrigationContext, request.OwnerProfile, request.FarmModelId,
            request.UpdateFarmModel), cancellationToken));
    }

    [HttpGet("farm-models", Name = "ListFarmModels")]
    [EndpointSummary("List Farm Models")]
    [EndpointDescription("Lists tenant-scoped active and inactive Farm Model categories.")]
    [ProducesResponseType<IReadOnlyList<FarmModelDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyList<FarmModelDto>>> ListFarmModels(CancellationToken cancellationToken)
        => Ok(await sender.Send(new GetFarmModelsQuery(), cancellationToken));

    [HttpPost("farm-models", Name = "SaveFarmModel")]
    [EndpointSummary("Save Farm Model")]
    [EndpointDescription("Creates or updates a tenant category; codes remain stable and categories are never deleted.")]
    [ProducesResponseType<FarmModelDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FarmModelDto>> SaveFarmModel(SaveFarmModelCommand request, CancellationToken cancellationToken)
        => Ok(await sender.Send(request, cancellationToken));

    [HttpPost("owner/national-id/reveal", Name = "RevealFarmOwnerNationalId")]
    [EndpointSummary("Reveal Farm Owner national ID")]
    [EndpointDescription("Grower-only audited reveal of protected Farm Owner identity.")]
    [ProducesResponseType<RevealedFarmOwnerNationalIdDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<RevealedFarmOwnerNationalIdDto>> RevealFarmOwnerNationalId(CancellationToken cancellationToken)
        => Ok(await sender.Send(new RevealFarmOwnerNationalIdCommand(), cancellationToken));

    [HttpPut("fields/{fieldId:guid}", Name = "UpdateFieldDetails")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<FarmSetupDto>> UpdateField(Guid fieldId, UpdateFieldDetailsRequest request,
        CancellationToken cancellationToken)
        => Ok(await sender.Send(new UpdateFieldDetailsCommand(fieldId, request.Name, request.IrrigationMethod,
            request.SoilNotes), cancellationToken));

    [HttpPost("fields")]
    [EndpointSummary("Create field")]
    [EndpointDescription("Adds a uniquely coded field to the authenticated grower's active farm.")]
    [ProducesResponseType<FarmSetupDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmSetupDto>> CreateField(
        CreateFieldRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new CreateFieldCommand(
            request.Code,
            request.Name,
            request.DeclaredHectares,
            request.MappedHectares,
            request.ReportingAreaSource,
            request.IrrigationMethod,
            request.SoilNotes), cancellationToken));
    }
}
