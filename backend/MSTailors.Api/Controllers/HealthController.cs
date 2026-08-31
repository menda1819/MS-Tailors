using Microsoft.AspNetCore.Mvc;
using MSTailors.Application.Health;

namespace MSTailors.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class HealthController : ControllerBase
{
    private readonly IHealthService _healthService;

    public HealthController(IHealthService healthService)
    {
        _healthService = healthService;
    }

    /// <summary>Liveness and MongoDB connectivity check.</summary>
    /// <response code="200">The API is up and MongoDB responded to a ping.</response>
    /// <response code="503">The API is up but MongoDB did not respond.</response>
    [HttpGet]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(HealthResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken cancellationToken)
    {
        var health = await _healthService.GetHealthAsync(cancellationToken);

        // A degraded database is a real failure for callers, so it must not be a 200.
        return health.Database.Status == "Connected"
            ? Ok(health)
            : StatusCode(StatusCodes.Status503ServiceUnavailable, health);
    }
}
