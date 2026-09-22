using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Inspections;

/// <summary>
/// Carregar a vistoria com as paradas e monta-la em resposta e o que todo caso de uso desta feature
/// faz na entrada e na saida. Extensao estatica pelo mesmo motivo de <c>ProtocolProjection</c>: nao
/// guarda estado, e nomea-la <c>...Handler</c> a faria parecer um ponto de entrada.
/// </summary>
internal static class InspectionProjection
{
    internal static async Task<Inspection> LoadInspectionAsync(
        this ISmartGraoDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var inspectionId = new InspectionId(id);

        return await db.Inspections
            .Include(x => x.Observations).ThenInclude(x => x.Counts)
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.NotFound);
    }

    internal static async Task<InspectionViewModel> ProjectAsync(
        this ISmartGraoDbContext db, Inspection inspection, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inspection);

        var responsible = await db.People.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inspection.ResponsibleId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.PersonNotFound);

        var protocol = await db.Protocols.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == inspection.ProtocolId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.NotFound);

        var targetIds = inspection.Observations
            .SelectMany(observation => observation.Counts)
            .Select(count => count.TargetId)
            .Distinct()
            .ToList();

        var targets = await db.MonitoringTargets.AsNoTracking()
            .Where(target => targetIds.Contains(target.Id))
            .ToDictionaryAsync(target => target.Id, cancellationToken);

        // A sequencia do ponto vem da malha: na tela, "parada 7" diz mais do que um id.
        var planId = inspection.SamplingPlanId;
        var plan = await db.SamplingPlans.AsNoTracking().Include(x => x.Points)
            .FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Sampling.NotFound);

        var sequences = plan.Points.ToDictionary(point => point.Id, point => point.Sequence);

        var observations = inspection.Observations
            .OrderBy(observation => observation.RecordedAt)
            .Select(observation => new ObservationViewModel(
                observation.Id.Value,
                observation.SamplingPointId?.Value,
                observation.SamplingPointId is { } pointId && sequences.TryGetValue(pointId, out var sequence)
                    ? sequence
                    : null,
                observation.RecordedAt,
                GeoCoordinateViewModel.From(observation.Coordinate),
                observation.AccuracyMeters,
                observation.HasPoorAccuracy,
                observation.GrowthStage,
                observation.Notes,
                observation.IsOffPlan,
                observation.Counts.Select(count => new TargetCountViewModel(
                    count.Id.Value, count.ProtocolItemId.Value, count.TargetId.Value,
                    targets.TryGetValue(count.TargetId, out var target) ? target.Code : string.Empty,
                    targets.TryGetValue(count.TargetId, out var named) ? named.CommonName : string.Empty,
                    count.Unit, count.Value, count.Detected))
                    .OrderBy(count => count.TargetCommonName, StringComparer.OrdinalIgnoreCase)
                    .ToList()))
            .ToList();

        return new InspectionViewModel(
            inspection.Id.Value, inspection.FieldId.Value, inspection.CultivationId.Value,
            inspection.SamplingPlanId.Value, inspection.ProtocolId.Value, protocol.Name,
            protocol.Version, inspection.ResponsibleId.Value, responsible.Name, inspection.Crop,
            inspection.ScheduledFor, inspection.Status, inspection.StartedAt, inspection.CompletedAt,
            inspection.CancellationReason, plan.PointCount, observations);
    }
}
