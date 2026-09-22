using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Inspections;
using SmartGrao.Domain.Inspections;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Route("api/inspections")]
[Produces("application/json")]
public sealed class InspectionsController : ControllerBase
{
    [HttpGet(Name = "GetInspections")]
    [ProducesResponseType(typeof(IReadOnlyList<InspectionSummaryViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InspectionSummaryViewModel>>> List(
        [FromQuery] Guid? cultivationId, [FromQuery] Guid? responsibleId,
        [FromQuery] InspectionStatus? status,
        [FromServices] GetInspectionsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(cultivationId, responsibleId, status, cancellationToken));

    [HttpGet("{id:guid}", Name = "GetInspectionById")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> Get(
        Guid id, [FromServices] GetInspectionByIdQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost(Name = "ScheduleInspection")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<InspectionViewModel>> Schedule(
        [FromBody] ScheduleInspectionViewModel model,
        [FromServices] ScheduleInspectionCommandHandler handler, CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetInspectionById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}/schedule", Name = "RescheduleInspection")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> Reschedule(
        Guid id, [FromBody] RescheduleInspectionViewModel model,
        [FromServices] RescheduleInspectionCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    [HttpPut("{id:guid}/start", Name = "StartInspection")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> Start(
        Guid id, [FromServices] StartInspectionCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    /// <summary>Registra uma parada. Sem <c>samplingPointId</c>, é uma ocorrência fora da malha.</summary>
    [HttpPost("{id:guid}/observations", Name = "RecordObservation")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> RecordObservation(
        Guid id, [FromBody] RecordObservationViewModel model,
        [FromServices] RecordObservationCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    [HttpPut("{id:guid}/completion", Name = "CompleteInspection")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> Complete(
        Guid id, [FromServices] CompleteInspectionCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("{id:guid}/cancellation", Name = "CancelInspection")]
    [ProducesResponseType(typeof(InspectionViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<InspectionViewModel>> Cancel(
        Guid id, [FromBody] CancelInspectionViewModel model,
        [FromServices] CancelInspectionCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));
}
