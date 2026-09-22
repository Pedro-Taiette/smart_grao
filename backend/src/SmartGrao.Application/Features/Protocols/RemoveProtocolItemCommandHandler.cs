using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class RemoveProtocolItemCommandHandler(ISmartGraoDbContext db)
{
    public async Task<ProtocolViewModel> HandleAsync(
        Guid id, Guid itemId, CancellationToken cancellationToken = default)
    {
        var protocol = await db.LoadProtocolAsync(id, cancellationToken);

        protocol.RemoveItem(new ProtocolItemId(itemId));
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
