using SmartGrao.Application.Common;
using SmartGrao.Application.Features.Analysis;

namespace SmartGrao.Application.Abstractions;

public sealed record SatelliteStatsResult(double Mean, double Median, double P10, double P90);

public sealed record SatelliteZoneResult(GeoJsonPolygon Geometry, double AreaHectares, double MeanDrop, bool DroppedSincePrevious);

public sealed record SatellitePassResult(
    DateOnly Date, string SceneId, double ValidFraction, string Status,
    SatelliteStatsResult? Ndvi, SatelliteStatsResult? Ndre, IReadOnlyList<SatelliteZoneResult> Zones);

/// <summary>O servico Python (analytics/service). Sem estado: recebe a tarefa, devolve o resultado.</summary>
public interface IAnalyticsClient
{
    Task<IReadOnlyList<SatellitePassResult>> GetSatellitePassesAsync(GeoJsonPolygon polygon, DateOnly start, DateOnly end, CancellationToken cancellationToken);

    Task<ModelCardViewModel> GetModelCardAsync(CancellationToken cancellationToken);
}
