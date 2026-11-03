using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Risk;

public readonly record struct SatellitePassId(Guid Value) : IStronglyTypedId
{
    public static SatellitePassId New() => new(Guid.CreateVersion7());
}

public enum SatellitePassStatus
{
    /// <summary>Nuvem, sombra ou falta de dado cobriram o talhao. Fica registrada: saber que o satelite nao viu e informacao.</summary>
    NoReading = 0,
    Ok,
}

public sealed record IndexStatistics(double Mean, double Median, double P10, double P90);

/// <summary>Area que destoa do resto do talhao numa passagem. Nao e diagnostico.</summary>
public sealed class AnomalyZone
{
    private AnomalyZone() { }

    public int Index { get; private set; }

    public Polygon Geometry { get; private set; } = null!;

    public double AreaHectares { get; private set; }

    /// <summary>Queda relativa media do NDRE em relacao a mediana do talhao (negativa).</summary>
    public double MeanDrop { get; private set; }

    public bool DroppedSincePrevious { get; private set; }

    internal static AnomalyZone Create(int index, Polygon geometry, double areaHectares, double meanDrop, bool droppedSincePrevious) =>
        new() { Index = index, Geometry = geometry, AreaHectares = areaHectares, MeanDrop = meanDrop, DroppedSincePrevious = droppedSincePrevious };
}

/// <summary>Uma passagem do Sentinel-2 sobre o talhao.</summary>
public sealed class SatellitePass : AggregateRoot<SatellitePassId>
{
    private readonly List<AnomalyZone> _zones = [];

    private SatellitePass() { }

    public FieldId FieldId { get; private set; }

    public DateOnly Date { get; private set; }

    public string SceneId { get; private set; } = string.Empty;

    public double ValidFraction { get; private set; }

    public SatellitePassStatus Status { get; private set; }

    public IndexStatistics? Ndvi { get; private set; }

    public IndexStatistics? Ndre { get; private set; }

    public IReadOnlyList<AnomalyZone> Zones => _zones.AsReadOnly();

    public static SatellitePass NoReading(FieldId fieldId, DateOnly date, string sceneId, double validFraction) =>
        new() { Id = SatellitePassId.New(), FieldId = fieldId, Date = date, SceneId = sceneId, ValidFraction = validFraction, Status = SatellitePassStatus.NoReading };

    public static SatellitePass Reading(
        FieldId fieldId, DateOnly date, string sceneId, double validFraction, IndexStatistics ndvi, IndexStatistics ndre,
        IEnumerable<(Polygon Geometry, double AreaHectares, double MeanDrop, bool DroppedSincePrevious)> zones)
    {
        var pass = new SatellitePass
        {
            Id = SatellitePassId.New(), FieldId = fieldId, Date = date, SceneId = sceneId,
            ValidFraction = validFraction, Status = SatellitePassStatus.Ok, Ndvi = ndvi, Ndre = ndre,
        };
        var index = 0;
        foreach (var zone in zones.OrderBy(x => x.MeanDrop))
            pass._zones.Add(AnomalyZone.Create(index++, zone.Geometry, zone.AreaHectares, zone.MeanDrop, zone.DroppedSincePrevious));
        return pass;
    }
}
