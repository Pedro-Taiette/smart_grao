namespace SmartGrao.Domain.Geo;

/// <summary>Sistemas de referencia usados no projeto.</summary>
public static class Srid
{
    /// <summary>
    /// WGS84 em graus — o que o GPS do celular emite, o que o Leaflet desenha e o que o GeoJSON
    /// exige (RFC 7946). Toda geometria que entra ou sai do sistema esta neste SRID; nao ha
    /// reprojecao em lugar nenhum, e por isso area e distancia sao calculadas por formula
    /// geodesica em vez de aritmetica plana sobre graus.
    /// </summary>
    public const int Wgs84 = 4326;
}
