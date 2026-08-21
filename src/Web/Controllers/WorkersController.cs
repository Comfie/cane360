using Cane360.Application.Labour;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/workers")]
public sealed class WorkersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkerListItemDto>>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkersQuery(), cancellationToken));

    [HttpGet("{workerId:guid}")]
    public async Task<ActionResult<WorkerDetailsDto>> GetById(Guid workerId, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetWorkerDetailsQuery(workerId), cancellationToken));

    [HttpPost]
    public async Task<ActionResult<WorkerDetailsDto>> Create(CreateWorkerRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ActiveFrom, out var activeFrom))
        {
            return BadRequest(DateError(nameof(request.ActiveFrom)));
        }

        var result = await sender.Send(new CreateWorkerCommand(request.PersonId, request.DisplayName,
            request.Phone, request.EmploymentType, activeFrom, request.NationalId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { workerId = result.Worker.Id }, result);
    }

    [HttpPost("{workerId:guid}/archive")]
    public async Task<ActionResult<WorkerDetailsDto>> Archive(Guid workerId, ArchiveWorkerRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ActiveTo, out var activeTo))
        {
            return BadRequest(DateError(nameof(request.ActiveTo)));
        }

        return Ok(await sender.Send(new ArchiveWorkerCommand(workerId, activeTo, request.ExpectedVersion), cancellationToken));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("{workerId:guid}/national-id/reveal")]
    public async Task<IActionResult> RevealNationalId(Guid workerId, RevealNationalIdRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RevealWorkerNationalIdCommand(workerId, request.Reason), cancellationToken);
        return Ok(new { result.WorkerId, result.NationalId });
    }

    private static ValidationProblemDetails DateError(string propertyName) => new(
        new Dictionary<string, string[]> { [propertyName] = ["Date must use yyyy-MM-dd."] });
}
