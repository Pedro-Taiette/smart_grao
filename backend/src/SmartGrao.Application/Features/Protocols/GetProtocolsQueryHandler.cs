using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class GetProtocolsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<ProtocolSummaryViewModel>> HandleAsync(
        Crop? crop = null, ProtocolStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = db.Protocols.AsNoTracking().Include(x => x.Items);

        var filtered = query.AsQueryable();
        if (crop is { } value) filtered = filtered.Where(x => x.Crop == value);
        if (status is { } protocolStatus) filtered = filtered.Where(x => x.Status == protocolStatus);

        var protocols = await filtered
            .OrderBy(x => x.Code).ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        return protocols.Select(ProtocolSummaryViewModel.From).ToList();
    }
}
