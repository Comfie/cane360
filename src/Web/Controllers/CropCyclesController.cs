using Cane360.Application.CropCycles;
using Cane360.Web.Models.CropCycles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/fields/{fieldId:guid}/crop-cycles")]
public sealed class CropCyclesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List field crop cycles")]
    [EndpointDescription("Returns current and historical crop cycles for a field in the authenticated tenant.")]
    [ProducesResponseType<CropCycleCollectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CropCycleCollectionDto>> Get(
        Guid fieldId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCropCyclesQuery(fieldId), cancellationToken));

    [HttpPost]
    [EndpointSummary("Create crop-cycle draft")]
    [EndpointDescription("Creates a plant-cane or ratoon crop-cycle draft for a field.")]
    [ProducesResponseType<CropCycleDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CropCycleDetailsDto>> Create(
        Guid fieldId,
        CreateCropCycleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCropCycleCommand(
            fieldId,
            request.CycleType,
            request.RatoonNumber,
            request.CropVarietyId,
            request.StartDate,
            request.ExpectedHarvestStart,
            request.ExpectedHarvestEnd,
            request.ExpectedYieldTonnes), cancellationToken);

        return CreatedAtAction(nameof(GetById), new { fieldId, cropCycleId = result.CropCycle.Id }, result);
    }

    [HttpGet("{cropCycleId:guid}")]
    [EndpointSummary("Get crop-cycle overview")]
    [ProducesResponseType<CropCycleDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CropCycleDetailsDto>> GetById(
        Guid fieldId,
        Guid cropCycleId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCropCycleDetailsQuery(fieldId, cropCycleId), cancellationToken));

    [HttpPost("{cropCycleId:guid}/transitions/activate")]
    public Task<ActionResult<CropCycleDetailsDto>> Activate(
        Guid fieldId, Guid cropCycleId, TransitionCropCycleRequest request, CancellationToken cancellationToken) =>
        Send(new ActivateCropCycleCommand(fieldId, cropCycleId, request.ExpectedVersion), cancellationToken);

    [HttpPost("{cropCycleId:guid}/transitions/cancel")]
    public Task<ActionResult<CropCycleDetailsDto>> Cancel(
        Guid fieldId, Guid cropCycleId, CancelCropCycleRequest request, CancellationToken cancellationToken) =>
        Send(new CancelCropCycleCommand(fieldId, cropCycleId, request.ExpectedVersion, request.Reason), cancellationToken);

    [HttpPost("{cropCycleId:guid}/transitions/ready-for-harvest")]
    public Task<ActionResult<CropCycleDetailsDto>> ReadyForHarvest(
        Guid fieldId, Guid cropCycleId, TransitionCropCycleRequest request, CancellationToken cancellationToken) =>
        Send(new MarkCropCycleReadyForHarvestCommand(fieldId, cropCycleId, request.ExpectedVersion), cancellationToken);

    [HttpPost("{cropCycleId:guid}/transitions/harvest")]
    public Task<ActionResult<CropCycleDetailsDto>> Harvest(
        Guid fieldId, Guid cropCycleId, HarvestCropCycleRequest request, CancellationToken cancellationToken) =>
        Send(new HarvestCropCycleCommand(
            fieldId, cropCycleId, request.ExpectedVersion, request.HarvestDate, request.ActualTonnes), cancellationToken);

    [HttpPost("{cropCycleId:guid}/transitions/close")]
    public Task<ActionResult<CropCycleDetailsDto>> Close(
        Guid fieldId, Guid cropCycleId, TransitionCropCycleRequest request, CancellationToken cancellationToken) =>
        Send(new CloseCropCycleCommand(fieldId, cropCycleId, request.ExpectedVersion), cancellationToken);

    private async Task<ActionResult<CropCycleDetailsDto>> Send(
        IRequest<CropCycleDetailsDto> command,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(command, cancellationToken));
}
