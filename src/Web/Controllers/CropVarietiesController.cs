using Cane360.Application.CropCycles;
using Cane360.Web.Models.CropCycles;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public sealed class CropVarietiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CropVarietyDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CropVarietyDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetCropVarietiesQuery(), cancellationToken));

    [HttpPost]
    [ProducesResponseType<CropVarietyDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CropVarietyDto>> Create(
        CreateCropVarietyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateCropVarietyCommand(request.Code, request.Name), cancellationToken);
        return Created(string.Empty, result);
    }
}
