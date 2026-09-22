using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Cultivations;
using SmartGrao.Domain.Fields;

namespace SmartGrao.WebApi.Controllers;

/// <summary>
/// A escala fenológica por cultura. É tabela de domínio, igual para todas as propriedades — por isso
/// fica fora de <c>/api/cultivations</c>, que trata do ciclo de um talhão.
/// </summary>
[ApiController]
[Route("api/growth-stages")]
[Produces("application/json")]
public sealed class GrowthStagesController : ControllerBase
{
    [HttpGet(Name = "GetGrowthStages")]
    [ProducesResponseType(typeof(IReadOnlyList<GrowthStageOptionViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<GrowthStageOptionViewModel>>> List(
        [FromQuery] Crop crop, [FromServices] GetGrowthStagesQueryHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(crop, cancellationToken));
}
