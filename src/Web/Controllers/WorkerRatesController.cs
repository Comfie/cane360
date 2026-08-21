using Cane360.Application.Labour;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/workers/{workerId:guid}/rates")]
public sealed class WorkerRatesController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkerDetailsDto>> Create(Guid workerId, CreateWorkerRateRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EffectiveFrom, out var effectiveFrom) ||
            !TransportValueParser.TryParseOptionalDateOnly(request.EffectiveTo, out var effectiveTo))
        {
            return BadRequest(DateError());
        }

        return Ok(await sender.Send(new CreateWorkerRateCommand(workerId, request.Basis, request.ActivityTypeId,
            request.RateUsd, effectiveFrom, effectiveTo), cancellationToken));
    }

    [HttpPost("{rateId:guid}/end")]
    public async Task<ActionResult<WorkerDetailsDto>> End(Guid workerId, Guid rateId, EndWorkerRateRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EffectiveTo, out var effectiveTo))
        {
            return BadRequest(DateError());
        }

        return Ok(await sender.Send(new EndWorkerRateCommand(workerId, rateId, effectiveTo, request.ExpectedVersion), cancellationToken));
    }

    private static ValidationProblemDetails DateError() => new(
        new Dictionary<string, string[]> { ["date"] = ["Date must use yyyy-MM-dd."] });
}
