using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Fields;
using SmartGrao.Application.Features.Sampling;

namespace SmartGrao.WebApi.Controllers;

/// <summary>
/// Talhoes: o desenho feito sobre o mapa vira geometria validada, medida em hectares e persistida
/// como <c>geography(Polygon,4326)</c>.
/// </summary>
[ApiController]
[Route("api/fields")]
[Produces("application/json")]
public sealed class FieldsController : ControllerBase
{
    [HttpGet("{id:guid}", Name = "GetFieldById")]
    [ProducesResponseType(typeof(FieldViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FieldViewModel>> GetById(
        Guid id,
        [FromServices] GetFieldByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    /// <summary>
    /// Cria o talhao a partir do poligono desenhado. Recusa contorno auto-interseccionado (422),
    /// area fora dos limites do dominio (400), nome repetido na fazenda e sobreposicao com talhao
    /// vizinho (409).
    /// </summary>
    [HttpPost(Name = "CreateField")]
    [ProducesResponseType(typeof(FieldViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FieldViewModel>> Create(
        [FromBody] CreateFieldViewModel model,
        [FromServices] CreateFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetFieldById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}", Name = "UpdateField")]
    [ProducesResponseType(typeof(FieldViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FieldViewModel>> Update(
        Guid id,
        [FromBody] UpdateFieldViewModel model,
        [FromServices] UpdateFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, model, cancellationToken));
    }

    /// <summary>
    /// Ativa ou desativa. Preferivel a exclusao para um talhao que ja tem historico: a area some da
    /// operacao mas o que foi diagnosticado nela continua existindo.
    /// </summary>
    [HttpPatch("{id:guid}/status", Name = "ChangeFieldStatus")]
    [ProducesResponseType(typeof(FieldViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FieldViewModel>> ChangeStatus(
        Guid id,
        [FromQuery] bool active,
        [FromServices] ChangeFieldStatusCommandHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, active, cancellationToken));
    }

    /// <summary>
    /// Historico de amostragem do talhao, do mais recente para o mais antigo. Devolve o resumo de
    /// cada plano, sem a malha — a malha se pede pelo plano.
    /// </summary>
    [HttpGet("{id:guid}/sampling-plans", Name = "GetSamplingPlansByField")]
    [ProducesResponseType(typeof(IReadOnlyList<SamplingPlanSummaryViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SamplingPlanSummaryViewModel>>> GetSamplingPlans(
        Guid id,
        [FromServices] GetSamplingPlansByFieldQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    [HttpDelete("{id:guid}", Name = "DeleteField")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteFieldCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
