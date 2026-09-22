using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class GetProtocolByIdQueryHandler(ISmartGraoDbContext db)
{
    public async Task<ProtocolViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var protocolId = new ProtocolId(id);

        var protocol = await db.Protocols.AsNoTracking().Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == protocolId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.NotFound);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
