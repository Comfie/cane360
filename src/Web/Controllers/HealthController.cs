using Cane360.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class HealthController(IDatabaseHealthCheck healthCheck) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet]
    [EndpointSummary("Health check")]
    [EndpointDescription("Reports whether the API can connect to PostgreSQL.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var canConnect = await healthCheck.CanConnectAsync(cancellationToken);

        return canConnect
            ? Ok(new { status = "healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
    }
}
