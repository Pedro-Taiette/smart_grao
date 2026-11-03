using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Application.Features.Risk;

public sealed record IndexStatisticsViewModel(double Mean, double Median, double P10, double P90);

public sealed record AnomalyZoneViewModel(GeoJsonPolygon Geometry, double AreaHectares, double MeanDrop, bool DroppedSincePrevious);

public sealed record SatellitePassViewModel(
    DateOnly Date, SatellitePassStatus Status, double ValidFraction,
    IndexStatisticsViewModel? Ndvi, IndexStatisticsViewModel? Ndre, IReadOnlyList<AnomalyZoneViewModel> Zones);

public sealed record SatelliteViewModel(Guid CultivationId, Guid FieldId, DateOnly From, DateOnly To, IReadOnlyList<SatellitePassViewModel> Passes);

internal static class SatelliteProjection
{
    public static (DateOnly From, DateOnly To) Range(Cultivation cultivation)
    {
        var today = RiskClock.Today;
        return (cultivation.PlantedOn, cultivation.EndedOn is { } ended && ended < today ? ended : today);
    }

    public static async Task<SatelliteViewModel> BuildAsync(ISmartGraoDbContext db, Cultivation cultivation, CancellationToken cancellationToken)
    {
        var (from, to) = Range(cultivation);
        var passes = await db.SatellitePasses.AsNoTracking()
            .Where(x => x.FieldId == cultivation.FieldId && x.Date >= from && x.Date <= to)
            .OrderBy(x => x.Date).ToListAsync(cancellationToken);
        return new SatelliteViewModel(cultivation.Id.Value, cultivation.FieldId.Value, from, to, passes.Select(ToViewModel).ToList());
    }

    private static SatellitePassViewModel ToViewModel(SatellitePass pass) => new(
        pass.Date, pass.Status, pass.ValidFraction, ToViewModel(pass.Ndvi), ToViewModel(pass.Ndre),
        pass.Zones.OrderBy(x => x.Index).Select(zone => new AnomalyZoneViewModel(
            GeoJsonPolygon.FromPolygon(zone.Geometry), zone.AreaHectares, zone.MeanDrop, zone.DroppedSincePrevious)).ToList());

    private static IndexStatisticsViewModel? ToViewModel(IndexStatistics? stats) =>
        stats is null ? null : new(stats.Mean, stats.Median, stats.P10, stats.P90);
}

public sealed class GetSatelliteQueryHandler(ISmartGraoDbContext db)
{
    public async Task<SatelliteViewModel> HandleAsync(Guid cultivationId, CancellationToken cancellationToken = default)
    {
        var cultivation = await GetCercosporaRiskQueryHandler.LoadCornCultivationAsync(db, cultivationId, cancellationToken);
        return await SatelliteProjection.BuildAsync(db, cultivation, cancellationToken);
    }
}

public sealed class RefreshSatelliteCommandHandler(ISmartGraoDbContext db, IAnalyticsClient analytics)
{
    public async Task<SatelliteViewModel> HandleAsync(Guid cultivationId, bool full = false, CancellationToken cancellationToken = default)
    {
        var cultivation = await GetCercosporaRiskQueryHandler.LoadCornCultivationAsync(db, cultivationId, cancellationToken);
        var field = await db.Fields.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultivation.FieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);
        var (from, to) = SatelliteProjection.Range(cultivation);

        // Recomeca na ultima passagem lida, para que a primeira passagem nova tenha com quem se comparar.
        var lastReading = await db.SatellitePasses.AsNoTracking()
            .Where(x => x.FieldId == cultivation.FieldId && x.Date >= from && x.Date <= to && x.Status == SatellitePassStatus.Ok)
            .MaxAsync(x => (DateOnly?)x.Date, cancellationToken);
        var start = full ? from : lastReading ?? from;

        IReadOnlyList<SatellitePassResult> results;
        try
        {
            results = await analytics.GetSatellitePassesAsync(GeoJsonPolygon.From(field.Boundary), start, to, cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new DomainException(SmartGraoErrors.Risk.AnalyticsUnavailable);
        }

        var dates = results.Select(x => x.Date).ToList();
        await db.SatellitePasses.Where(x => x.FieldId == cultivation.FieldId && dates.Contains(x.Date)).ExecuteDeleteAsync(cancellationToken);
        foreach (var result in results)
            db.SatellitePasses.Add(ToDomain(cultivation, result));
        await db.SaveChangesAsync(cancellationToken);
        return await SatelliteProjection.BuildAsync(db, cultivation, cancellationToken);
    }

    private static SatellitePass ToDomain(Cultivation cultivation, SatellitePassResult result)
    {
        if (result.Status != "ok" || result.Ndvi is null || result.Ndre is null)
            return SatellitePass.NoReading(cultivation.FieldId, result.Date, result.SceneId, result.ValidFraction);
        var zones = new List<(NetTopologySuite.Geometries.Polygon, double, double, bool)>();
        foreach (var zone in result.Zones)
        {
            try
            {
                zones.Add((zone.Geometry.ToBoundary().Polygon, zone.AreaHectares, zone.MeanDrop, zone.DroppedSincePrevious));
            }
            catch (DomainException)
            {
                // Zona degenerada depois da simplificacao: sem geometria valida, nao ha onde caminhar.
            }
        }
        return SatellitePass.Reading(cultivation.FieldId, result.Date, result.SceneId, result.ValidFraction,
            new IndexStatistics(result.Ndvi.Mean, result.Ndvi.Median, result.Ndvi.P10, result.Ndvi.P90),
            new IndexStatistics(result.Ndre.Mean, result.Ndre.Median, result.Ndre.P10, result.Ndre.P90), zones);
    }
}
