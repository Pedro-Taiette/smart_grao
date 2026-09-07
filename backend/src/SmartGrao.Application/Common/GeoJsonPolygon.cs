using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Application.Common;

/// <summary>
/// Uma geometria GeoJSON do tipo <c>Polygon</c> (RFC 7946) — exatamente o que o Leaflet-Geoman
/// produz quando o produtor fecha o desenho, e exatamente o que ele consome para redesenhar.
/// <para>
/// O tipo do NetTopologySuite nao e exposto no contrato de proposito: ele geraria um esquema
/// OpenAPI cheio de detalhes internos (fabrica, modelo de precisao, sequencia de coordenadas) que o
/// cliente nao deve conhecer nem enviar. O que atravessa a fronteira e o documento GeoJSON, que
/// qualquer ferramenta de SIG le.
/// </para>
/// </summary>
/// <param name="Type">Sempre <c>"Polygon"</c>.</param>
/// <param name="Coordinates">
/// Aneis, cada anel uma lista de posicoes, cada posicao <c>[longitude, latitude]</c> — nesta ordem,
/// que e a do GeoJSON e a inversa de como se fala. Aqui so o anel externo e aceito.
/// </param>
public sealed record GeoJsonPolygon(string Type, IReadOnlyList<IReadOnlyList<IReadOnlyList<double>>> Coordinates)
{
    public const string ExpectedType = "Polygon";

    /// <summary>Converte para o objeto de valor do dominio, que valida e mede.</summary>
    public Boundary ToBoundary()
    {
        if (!string.Equals(Type, ExpectedType, StringComparison.OrdinalIgnoreCase))
            throw new DomainException(SmartGraoErrors.Geo.UnsupportedGeoJson);

        if (Coordinates is null || Coordinates.Count == 0)
            throw new DomainException(SmartGraoErrors.Geo.EmptyBoundary);

        // Aneis interiores (buracos) sao recusados no dominio; recusar aqui tambem evita descartar
        // silenciosamente parte do que o usuario desenhou.
        if (Coordinates.Count > 1)
            throw new DomainException(SmartGraoErrors.Geo.PolygonHasHoles);

        var ring = Coordinates[0];
        if (ring is null || ring.Count == 0)
            throw new DomainException(SmartGraoErrors.Geo.EmptyBoundary);

        var vertices = new List<GeoCoordinate>(ring.Count);

        foreach (var position in ring)
        {
            // Uma posicao GeoJSON pode trazer altitude como terceiro elemento; ela e ignorada, mas
            // menos de dois numeros nao e uma posicao.
            if (position is null || position.Count < 2)
                throw new DomainException(SmartGraoErrors.Geo.MalformedGeoJson);

            vertices.Add(GeoCoordinate.FromGeoJsonPair(position[0], position[1]));
        }

        return Boundary.FromCoordinates(vertices);
    }

    public static GeoJsonPolygon From(Boundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        // O anel sai fechado (a ultima posicao repete a primeira), como a RFC exige.
        var ring = boundary.Polygon.ExteriorRing.Coordinates
            .Select(c => (IReadOnlyList<double>)new[] { c.X, c.Y })
            .ToArray();

        return new GeoJsonPolygon(ExpectedType, [ring]);
    }
}
