using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;

namespace SmartGrao.Application.Features.Inspections;

public sealed class GetInspectionsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<InspectionSummaryViewModel>> HandleAsync(
        Guid? cultivationId = null, Guid? responsibleId = null, InspectionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.Inspections.AsNoTracking().Include(x => x.Observations).AsQueryable();

        if (cultivationId is { } rawCultivation)
        {
            var id = new CultivationId(rawCultivation);
            query = query.Where(x => x.CultivationId == id);
        }

        if (responsibleId is { } rawResponsible)
        {
            var id = new PersonId(rawResponsible);
            query = query.Where(x => x.ResponsibleId == id);
        }

        if (status is { } inspectionStatus)
            query = query.Where(x => x.Status == inspectionStatus);

        var inspections = await query
            .OrderByDescending(x => x.ScheduledFor).ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        if (inspections.Count == 0) return [];

        // Nome do responsavel e tamanho da malha entram por fora: a lista e a agenda da equipe, e
        // "Ana, 12 paradas" e o que se le ali — nao dois ids.
        var responsibleIds = inspections.Select(x => x.ResponsibleId).Distinct().ToList();
        var names = await db.People.AsNoTracking()
            .Where(person => responsibleIds.Contains(person.Id))
            .ToDictionaryAsync(person => person.Id, person => person.Name, cancellationToken);

        var planIds = inspections.Select(x => x.SamplingPlanId).Distinct().ToList();
        var pointCounts = await db.SamplingPlans.AsNoTracking()
            .Where(plan => planIds.Contains(plan.Id))
            .Select(plan => new { plan.Id, Count = plan.Points.Count })
            .ToDictionaryAsync(plan => plan.Id, plan => plan.Count, cancellationToken);

        return inspections.Select(inspection => new InspectionSummaryViewModel(
            inspection.Id.Value, inspection.FieldId.Value, inspection.CultivationId.Value,
            inspection.SamplingPlanId.Value, inspection.ProtocolId.Value,
            inspection.ResponsibleId.Value,
            names.TryGetValue(inspection.ResponsibleId, out var name) ? name : string.Empty,
            inspection.Crop, inspection.ScheduledFor, inspection.Status, inspection.StartedAt,
            inspection.CompletedAt, inspection.CancellationReason,
            inspection.Observations.Count, inspection.VisitedPointCount,
            pointCounts.TryGetValue(inspection.SamplingPlanId, out var points) ? points : 0))
            .ToList();
    }
}

public sealed class GetInspectionByIdQueryHandler(ISmartGraoDbContext db)
{
    public async Task<InspectionViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var inspectionId = new InspectionId(id);

        var inspection = await db.Inspections.AsNoTracking()
            .Include(x => x.Observations).ThenInclude(x => x.Counts)
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.NotFound);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}
