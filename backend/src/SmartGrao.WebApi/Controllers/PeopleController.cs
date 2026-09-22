using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Inspections;

namespace SmartGrao.WebApi.Controllers;

/// <summary>
/// A equipe de campo. Cadastro simples, sem login: a vistoria precisa de um responsável com
/// identidade estável muito antes de o sistema ter autenticação.
/// </summary>
[ApiController]
[Route("api/people")]
[Produces("application/json")]
public sealed class PeopleController : ControllerBase
{
    [HttpGet(Name = "GetPeople")]
    [ProducesResponseType(typeof(IReadOnlyList<PersonViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PersonViewModel>>> List(
        [FromQuery] Guid farmId, [FromQuery] bool activeOnly,
        [FromServices] GetPeopleQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(farmId, activeOnly, cancellationToken));

    [HttpPost(Name = "CreatePerson")]
    [ProducesResponseType(typeof(PersonViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<PersonViewModel>> Create(
        [FromBody] CreatePersonViewModel model, [FromServices] CreatePersonCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetPeople", new { farmId = created.FarmId }, created);
    }

    [HttpPut("{id:guid}", Name = "UpdatePerson")]
    [ProducesResponseType(typeof(PersonViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonViewModel>> Update(
        Guid id, [FromBody] UpdatePersonViewModel model,
        [FromServices] UpdatePersonCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    /// <summary>Tira ou devolve alguém à equipe. Não há exclusão: o histórico aponta para a pessoa.</summary>
    [HttpPut("{id:guid}/status", Name = "SetPersonStatus")]
    [ProducesResponseType(typeof(PersonViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<PersonViewModel>> SetStatus(
        Guid id, [FromQuery] bool active, [FromServices] SetPersonStatusCommandHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, active, cancellationToken));
}
