using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Protocols;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Route("api/monitoring-targets")]
[Produces("application/json")]
public sealed class MonitoringTargetsController : ControllerBase
{
    [HttpGet(Name = "GetMonitoringTargets")]
    [ProducesResponseType(typeof(IReadOnlyList<MonitoringTargetViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MonitoringTargetViewModel>>> List(
        [FromQuery] Crop? crop, [FromQuery] TargetKind? kind,
        [FromServices] GetMonitoringTargetsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(crop, kind, cancellationToken));

    [HttpPost(Name = "CreateMonitoringTarget")]
    [ProducesResponseType(typeof(MonitoringTargetViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<MonitoringTargetViewModel>> Create(
        [FromBody] CreateMonitoringTargetViewModel model,
        [FromServices] CreateMonitoringTargetCommandHandler handler, CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetMonitoringTargets", new { crop = created.Crop }, created);
    }

    /// <summary>
    /// Move o alvo entre registro manual, validacao e automacao. Habilitar automacao sem passar pela
    /// validacao e recusado pelo dominio.
    /// </summary>
    [HttpPut("{id:guid}/automation", Name = "ChangeTargetAutomation")]
    [ProducesResponseType(typeof(MonitoringTargetViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<MonitoringTargetViewModel>> ChangeAutomation(
        Guid id, [FromBody] ChangeAutomationViewModel model,
        [FromServices] ChangeAutomationCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));
}
