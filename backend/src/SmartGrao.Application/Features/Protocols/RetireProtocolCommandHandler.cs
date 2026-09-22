using SmartGrao.Application.Abstractions;

namespace SmartGrao.Application.Features.Protocols;

public sealed class RetireProtocolCommandHandler(ISmartGraoDbContext db)
{
    public async Task<ProtocolViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var protocol = await db.LoadProtocolAsync(id, cancellationToken);

        protocol.Retire();
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
