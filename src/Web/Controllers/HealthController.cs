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
    public Task<IActionResult> Get(CancellationToken cancellationToken) =>
        GetReady(cancellationToken);

    [AllowAnonymous]
    [HttpGet("live")]
    [EndpointSummary("Liveness check")]
    [EndpointDescription("Reports whether the API process is available without checking dependencies.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetLive() => Ok(new { status = "healthy" });

    [AllowAnonymous]
    [HttpGet("ready")]
    [EndpointSummary("Readiness check")]
    [EndpointDescription("Reports whether the API can connect to PostgreSQL.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady(CancellationToken cancellationToken)
    {
        bool canConnect;

        try
        {
            canConnect = await healthCheck.CanConnectAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            canConnect = false;
        }

        return canConnect
            ? Ok(new { status = "healthy" })
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "unhealthy" });
    }
}
