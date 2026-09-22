using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Cultivations;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Route("api/cultivations")]
[Produces("application/json")]
public sealed class CultivationsController : ControllerBase
{
    [HttpGet(Name = "GetCultivations")]
    [ProducesResponseType(typeof(IReadOnlyList<CultivationViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CultivationViewModel>>> List(
        [FromQuery] Guid fieldId, [FromServices] GetCultivationsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(fieldId, cancellationToken));

    [HttpGet("{id:guid}", Name = "GetCultivationById")]
    [ProducesResponseType(typeof(CultivationViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CultivationViewModel>> Get(
        Guid id, [FromServices] GetCultivationByIdQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost(Name = "CreateCultivation")]
    [ProducesResponseType(typeof(CultivationViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<CultivationViewModel>> Create(
        [FromBody] CreateCultivationViewModel model, [FromServices] CreateCultivationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetCultivationById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}/closure", Name = "CloseCultivation")]
    [ProducesResponseType(typeof(CultivationViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CultivationViewModel>> Close(
        Guid id, [FromBody] CloseCultivationViewModel model,
        [FromServices] CloseCultivationCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    [HttpPost("{id:guid}/stages", Name = "RecordGrowthStage")]
    [ProducesResponseType(typeof(CultivationViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<CultivationViewModel>> RecordStage(
        Guid id, [FromBody] RecordGrowthStageViewModel model,
        [FromServices] RecordGrowthStageCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));
}
