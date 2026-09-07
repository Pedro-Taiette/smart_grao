using System.Globalization;
using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Geo;

/// <summary>
/// Um ponto na superficie da Terra em WGS84. Objeto de valor: nao tem identidade, e imutavel e
/// nasce validado — nao existe <c>GeoCoordinate</c> com latitude 120.
/// </summary>
/// <remarks>
/// A ordem dos argumentos e (latitude, longitude), como se fala. O GeoJSON usa a ordem inversa,
/// (longitude, latitude), e essa troca e a origem classica de talhoes que aparecem no oceano — por
/// isso a conversao acontece num unico lugar, em <see cref="ToPoint"/> e nas fabricas do
/// <see cref="Boundary"/>, e nunca no meio de um caso de uso.
/// </remarks>
public sealed record GeoCoordinate
{
    private GeoCoordinate(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static GeoCoordinate From(double latitude, double longitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude) ||
            double.IsNaN(longitude) || double.IsInfinity(longitude))
        {
            throw new DomainException(SmartGraoErrors.Geo.CoordinateNotFinite);
        }

        if (latitude is < -90 or > 90)
            throw new DomainException(SmartGraoErrors.Geo.LatitudeOutOfRange);

        if (longitude is < -180 or > 180)
            throw new DomainException(SmartGraoErrors.Geo.LongitudeOutOfRange);

        return new GeoCoordinate(latitude, longitude);
    }

    /// <summary>Le um par GeoJSON, que vem como <c>[longitude, latitude]</c>.</summary>
    public static GeoCoordinate FromGeoJsonPair(double longitude, double latitude) =>
        From(latitude, longitude);

    public static GeoCoordinate FromCoordinate(Coordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        return From(coordinate.Y, coordinate.X);
    }

    /// <summary>Distancia geodesica ate outra coordenada, em metros.</summary>
    public double DistanceInMetersTo(GeoCoordinate other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Wgs84Geodesy.DistanceInMeters(this, other);
    }

    /// <summary>Ponto NTS correspondente. Em NTS, X e a longitude e Y e a latitude.</summary>
    public Point ToPoint() => new(new Coordinate(Longitude, Latitude)) { SRID = Srid.Wgs84 };

    /// <summary>Par na ordem do GeoJSON: <c>[longitude, latitude]</c>.</summary>
    public double[] ToGeoJsonPair() => [Longitude, Latitude];

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Latitude:F6}, {Longitude:F6}");
}
