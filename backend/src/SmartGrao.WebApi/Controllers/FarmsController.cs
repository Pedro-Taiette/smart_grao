using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Farms;
using SmartGrao.Application.Features.Fields;

namespace SmartGrao.WebApi.Controllers;

/// <summary>Fazendas e os talhoes que vivem sob elas (Pilar 1).</summary>
[ApiController]
[Route("api/farms")]
[Produces("application/json")]
public sealed class FarmsController : ControllerBase
{
    // O nome da rota vira o operationId no OpenAPI, e o operationId vira o nome do metodo no
    // cliente gerado para o frontend.
    [HttpGet(Name = "GetFarms")]
    [ProducesResponseType(typeof(IReadOnlyList<FarmViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FarmViewModel>>> GetAll(
        [FromServices] GetFarmsQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(cancellationToken));
    }

    [HttpGet("{id:guid}", Name = "GetFarmById")]
    [ProducesResponseType(typeof(FarmViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmViewModel>> GetById(
        Guid id,
        [FromServices] GetFarmByIdQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, cancellationToken));
    }

    /// <summary>Talhoes desta fazenda, cada um com o contorno em GeoJSON pronto para o mapa.</summary>
    [HttpGet("{id:guid}/fields", Name = "GetFieldsByFarm")]
    [ProducesResponseType(typeof(IReadOnlyList<FieldViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FieldViewModel>>> GetFields(
        Guid id,
        [FromQuery] bool activeOnly,
        [FromServices] GetFieldsByFarmQueryHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, activeOnly, cancellationToken));
    }

    [HttpPost(Name = "CreateFarm")]
    [ProducesResponseType(typeof(FarmViewModel), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<FarmViewModel>> Create(
        [FromBody] SaveFarmViewModel model,
        [FromServices] CreateFarmCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetFarmById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}", Name = "UpdateFarm")]
    [ProducesResponseType(typeof(FarmViewModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FarmViewModel>> Update(
        Guid id,
        [FromBody] SaveFarmViewModel model,
        [FromServices] UpdateFarmCommandHandler handler,
        CancellationToken cancellationToken)
    {
        return Ok(await handler.HandleAsync(id, model, cancellationToken));
    }

    [HttpDelete("{id:guid}", Name = "DeleteFarm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteFarmCommandHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
