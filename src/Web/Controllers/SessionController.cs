using Cane360.Application.Session;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/session")]
public sealed class SessionController(ISender sender) : ControllerBase
{
    [HttpGet(Name = "GetCurrentSession")]
    public async Task<ActionResult<SessionSummaryDto>> Get(CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetSessionQuery(), cancellationToken));
}
