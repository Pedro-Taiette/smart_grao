using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Cultivations;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Route("api/seasons")]
[Produces("application/json")]
public sealed class SeasonsController : ControllerBase
{
    [HttpGet(Name = "GetSeasons")]
    [ProducesResponseType(typeof(IReadOnlyList<SeasonViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SeasonViewModel>>> List(
        [FromQuery] Guid farmId, [FromServices] GetSeasonsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(farmId, cancellationToken));

    [HttpPost(Name = "CreateSeason")]
    [ProducesResponseType(typeof(SeasonViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<SeasonViewModel>> Create(
        [FromBody] CreateSeasonViewModel model, [FromServices] CreateSeasonCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetSeasons", new { farmId = created.FarmId }, created);
    }
}
