using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class GetMonitoringTargetsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<MonitoringTargetViewModel>> HandleAsync(
        Crop? crop = null, TargetKind? kind = null, CancellationToken cancellationToken = default)
    {
        var query = db.MonitoringTargets.AsNoTracking();

        if (crop is { } value) query = query.Where(x => x.Crop == value);
        if (kind is { } targetKind) query = query.Where(x => x.Kind == targetKind);

        var targets = await query
            .OrderBy(x => x.Kind).ThenBy(x => x.CommonName)
            .ToListAsync(cancellationToken);

        return targets.Select(MonitoringTargetViewModel.From).ToList();
    }
}
