using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Features.Risk;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Risk;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

public sealed class CreateDirectedPlanCommandHandler(ISmartGraoDbContext db)
{
    /// <summary>Zona mais velha que isso ja nao diz onde a lavoura esta hoje; a malha cobre o talhao inteiro.</summary>
    public const int ZoneValidDays = 21;

    public async Task<SamplingPlanViewModel> HandleAsync(Guid cultivationId, CancellationToken cancellationToken = default)
    {
        var cultivation = await GetCercosporaRiskQueryHandler.LoadCornCultivationAsync(db, cultivationId, cancellationToken);
        if (cultivation.EndedOn is not null) throw new DomainException(SmartGraoErrors.Cultivation.Closed);
        var field = await db.Fields.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultivation.FieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);
        if (!field.Active) throw new DomainException(SmartGraoErrors.Sampling.FieldIsInactive);

        var since = RiskClock.Today.AddDays(-ZoneValidDays);
        var pass = await db.SatellitePasses.AsNoTracking()
            .Where(x => x.FieldId == cultivation.FieldId && x.Status == SatellitePassStatus.Ok && x.Date >= since && x.Zones.Any())
            .OrderByDescending(x => x.Date).FirstOrDefaultAsync(cancellationToken);
        var zones = pass?.Zones.Select(zone => Boundary.FromPolygon(zone.Geometry)).ToList() ?? [];

        var plan = SamplingPlan.Directed(cultivation.FieldId, field.Boundary, zones, pass?.Date, cultivation.Id);
        db.SamplingPlans.Add(plan);
        await db.SaveChangesAsync(cancellationToken);
        return SamplingPlanViewModel.From(plan);
    }
}
