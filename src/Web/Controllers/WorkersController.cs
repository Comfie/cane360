using Cane360.Application.Labour;
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
    [HttpGet(Name = "GetWorkers")]
    public async Task<ActionResult<IReadOnlyList<WorkerListItemDto>>> Get(CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetWorkersQuery(), cancellationToken));
    }

    [HttpGet("{workerId:guid}", Name = "GetWorkerDetails")]
    public async Task<ActionResult<WorkerDetailsDto>> GetById(Guid workerId, CancellationToken cancellationToken)
    {
        return Ok(await sender.Send(new GetWorkerDetailsQuery(workerId), cancellationToken));
    }

    [HttpPost(Name = "CreateWorkers")]
    [ProducesResponseType<WorkerDetailsDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<WorkerDetailsDto>> Create(CreateWorkerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ActiveFrom, out DateOnly activeFrom))
        {
            return this.DateValidationError(nameof(request.ActiveFrom));
        }

        WorkerDetailsDto result = await sender.Send(new CreateWorkerCommand(request.PersonId, request.DisplayName,
            request.Phone, request.EmploymentType, activeFrom, request.NationalId, request.Profile), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { workerId = result.Worker.Id }, result);
    }

    [HttpPut("{workerId:guid}/profile", Name = "UpdateWorkerProfile")]
    public async Task<ActionResult<WorkerDetailsDto>> UpdateProfile(Guid workerId, UpdateWorkerProfileRequest request,
        CancellationToken cancellationToken) => Ok(await sender.Send(new UpdateWorkerProfileCommand(workerId,
            request.ExpectedVersion, request.ExpectedPersonVersion, request.DisplayName, request.Phone,
            request.EmploymentType, request.Profile), cancellationToken));

    [HttpPut("{workerId:guid}/national-id", Name = "CorrectWorkerNationalId")]
    public async Task<ActionResult<WorkerDetailsDto>> CorrectNationalId(Guid workerId, CorrectWorkerNationalIdRequest request,
        CancellationToken cancellationToken) => Ok(await sender.Send(new CorrectWorkerNationalIdCommand(workerId,
            request.NationalId, request.ExpectedVersion, request.Reason), cancellationToken));

    [HttpPost("{workerId:guid}/archive", Name = "ArchiveWorkers")]
    public async Task<ActionResult<WorkerDetailsDto>> Archive(Guid workerId, ArchiveWorkerRequest request,
        CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.ActiveTo, out DateOnly activeTo))
        {
            return this.DateValidationError(nameof(request.ActiveTo));
        }

        return Ok(await sender.Send(new ArchiveWorkerCommand(workerId, activeTo, request.ExpectedVersion),
            cancellationToken));
    }

    [HttpPost("{workerId:guid}/national-id/reveal", Name = "RevealWorkerNationalId")]
    public async Task<ActionResult<RevealedNationalIdDto>> RevealNationalId(Guid workerId, RevealNationalIdRequest request,
        CancellationToken cancellationToken)
    {
        RevealedNationalIdDto result = await sender.Send(new RevealWorkerNationalIdCommand(workerId, request.Reason),
            cancellationToken);
        return Ok(result);
    }
}
