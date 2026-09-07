using SmartGrao.Domain.Geo;

namespace SmartGrao.Application.Common;

/// <summary>
/// Um ponto no contrato da API, na ordem em que se fala: latitude primeiro. Diferente do GeoJSON,
/// que exige <c>[longitude, latitude]</c> — aqui os campos sao nomeados justamente para que nao
/// exista ordem a errar.
/// </summary>
public sealed record GeoCoordinateViewModel(double Latitude, double Longitude)
{
    public static GeoCoordinateViewModel From(GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        return new GeoCoordinateViewModel(coordinate.Latitude, coordinate.Longitude);
    }

    public GeoCoordinate ToDomain() => GeoCoordinate.From(Latitude, Longitude);
}
