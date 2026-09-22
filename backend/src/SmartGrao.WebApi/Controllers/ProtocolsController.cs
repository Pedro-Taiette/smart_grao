using Microsoft.AspNetCore.Mvc;
using SmartGrao.Application.Features.Protocols;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.WebApi.Controllers;

[ApiController]
[Route("api/protocols")]
[Produces("application/json")]
public sealed class ProtocolsController : ControllerBase
{
    [HttpGet(Name = "GetProtocols")]
    [ProducesResponseType(typeof(IReadOnlyList<ProtocolSummaryViewModel>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProtocolSummaryViewModel>>> List(
        [FromQuery] Crop? crop, [FromQuery] ProtocolStatus? status,
        [FromServices] GetProtocolsQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(crop, status, cancellationToken));

    [HttpGet("{id:guid}", Name = "GetProtocolById")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> Get(
        Guid id, [FromServices] GetProtocolByIdQueryHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost(Name = "CreateProtocol")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProtocolViewModel>> Create(
        [FromBody] CreateProtocolViewModel model,
        [FromServices] CreateProtocolCommandHandler handler, CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(model, cancellationToken);
        return CreatedAtRoute("GetProtocolById", new { id = created.Id }, created);
    }

    /// <summary>Abre a proxima versao a partir desta, publicada, copiando os alvos.</summary>
    [HttpPost("{id:guid}/versions", Name = "CreateProtocolVersion")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProtocolViewModel>> CreateVersion(
        Guid id, [FromServices] CreateProtocolVersionCommandHandler handler, CancellationToken cancellationToken)
    {
        var created = await handler.HandleAsync(id, cancellationToken);
        return CreatedAtRoute("GetProtocolById", new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}/name", Name = "RenameProtocol")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> Rename(
        Guid id, [FromBody] RenameProtocolViewModel model,
        [FromServices] RenameProtocolCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    [HttpPost("{id:guid}/items", Name = "AddProtocolItem")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> AddItem(
        Guid id, [FromBody] AddProtocolItemViewModel model,
        [FromServices] AddProtocolItemCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, model, cancellationToken));

    [HttpDelete("{id:guid}/items/{itemId:guid}", Name = "RemoveProtocolItem")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> RemoveItem(
        Guid id, Guid itemId,
        [FromServices] RemoveProtocolItemCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, itemId, cancellationToken));

    [HttpPut("{id:guid}/publication", Name = "PublishProtocol")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> Publish(
        Guid id, [FromServices] PublishProtocolCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("{id:guid}/retirement", Name = "RetireProtocol")]
    [ProducesResponseType(typeof(ProtocolViewModel), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProtocolViewModel>> Retire(
        Guid id, [FromServices] RetireProtocolCommandHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));
}
