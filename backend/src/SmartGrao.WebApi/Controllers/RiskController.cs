using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Risk;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class RiskController : ControllerBase
{
    [HttpGet("api/cultivations/{id:guid}/cercospora-risk", Name = "GetCercosporaRisk")]
    [ProducesResponseType(typeof(CercosporaRiskViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CercosporaRiskViewModel>> Get(
        Guid id, [FromServices] GetCercosporaRiskQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost("api/cultivations/{id:guid}/weather/refresh", Name = "RefreshWeather")]
    [ProducesResponseType(typeof(CercosporaRiskViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CercosporaRiskViewModel>> Refresh(
        Guid id, [FromServices] RefreshWeatherCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpGet("api/cercospora-alerts", Name = "GetCercosporaAlerts")]
    [ProducesResponseType(typeof(IReadOnlyList<CercosporaAlertViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CercosporaAlertViewModel>>> Alerts(
        [FromQuery] Guid farmId, [FromServices] GetCercosporaAlertsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(farmId, cancellationToken));
}
