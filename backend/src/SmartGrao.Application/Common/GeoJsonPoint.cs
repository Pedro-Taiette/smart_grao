using SmartGrao.Domain.Geo;

namespace SmartGrao.Application.Common;

/// <summary>
/// Uma geometria GeoJSON do tipo <c>Point</c> (RFC 7946). E o que o Leaflet consome para plotar um
/// marcador, sem conversao no meio.
/// </summary>
/// <param name="Type">Sempre <c>"Point"</c>.</param>
/// <param name="Coordinates"><c>[longitude, latitude]</c> — a ordem do GeoJSON, inversa de como se fala.</param>
public sealed record GeoJsonPoint(string Type, IReadOnlyList<double> Coordinates)
{
    public const string ExpectedType = "Point";

    public static GeoJsonPoint From(GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);

        return new GeoJsonPoint(ExpectedType, coordinate.ToGeoJsonPair());
    }
}
