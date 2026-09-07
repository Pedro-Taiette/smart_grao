using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Geometries.Utilities;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Geo;

/// <summary>
/// O contorno fechado de um talhao: um poligono valido em WGS84, ja medido.
/// <para>
/// E o guardiao da geometria. Todo poligono que entra no sistema — vindo do desenho no mapa, de um
/// shapefile importado ou de um teste — passa por <see cref="FromCoordinates"/> ou
/// <see cref="FromPolygon"/>, e sai de la valido ou nao sai. Nenhum agregado aceita um
/// <c>Polygon</c> cru.
/// </para>
/// </summary>
public sealed class Boundary : IEquatable<Boundary>
{
    /// <summary>
    /// Um poligono precisa de tres vertices distintos mais a repeticao do primeiro para fechar.
    /// Com menos que isso a figura nao tem area.
    /// </summary>
    public const int MinimumPositions = 4;

    /// <summary>
    /// Teto de sanidade. Um talhao desenhado a mao tem dezenas de vertices; milhares indicam um
    /// contorno importado sem simplificacao, que trava o desenho no navegador e engorda cada
    /// consulta espacial sem tornar a medida mais exata.
    /// </summary>
    public const int MaximumPositions = 5_000;

    private static readonly GeometryFactory Factory =
        NtsGeometryServices.Instance.CreateGeometryFactory(Srid.Wgs84);

    private Boundary(Polygon polygon, decimal areaHectares, double perimeterMeters)
    {
        Polygon = polygon;
        AreaHectares = areaHectares;
        PerimeterMeters = perimeterMeters;
    }

    /// <summary>O poligono em si, SRID 4326, anel externo apenas.</summary>
    public Polygon Polygon { get; }

    /// <summary>Area geodesica em hectares — a medida que o produtor reconhece.</summary>
    public decimal AreaHectares { get; }

    public double PerimeterMeters { get; }

    /// <summary>
    /// Centroide, usado para centralizar o mapa e para ancorar o rotulo do talhao. Calculado no
    /// plano: como referencia visual dentro de um talhao a diferenca para o centroide geodesico e
    /// de centimetros.
    /// </summary>
    public GeoCoordinate Center => GeoCoordinate.FromCoordinate(Polygon.Centroid.Coordinate);

    public int VertexCount => Polygon.ExteriorRing.NumPoints - 1;

    /// <summary>Vertices do anel externo, sem repetir o ponto de fechamento.</summary>
    public IReadOnlyList<GeoCoordinate> Vertices =>
        [.. Polygon.ExteriorRing.Coordinates[..^1].Select(GeoCoordinate.FromCoordinate)];

    /// <summary>Caixa envolvente — o ponto de partida da malha amostral do Pilar 2.</summary>
    public Envelope BoundingBox => Polygon.EnvelopeInternal;

    /// <summary>
    /// Constroi a partir da lista de vertices do anel externo. Fecha o anel automaticamente quando
    /// o ultimo ponto nao repete o primeiro: o Leaflet-Geoman entrega o contorno aberto, e exigir
    /// que o frontend lembre de fechar seria transferir uma regra de topologia para a tela.
    /// </summary>
    public static Boundary FromCoordinates(IReadOnlyList<GeoCoordinate> vertices)
    {
        ArgumentNullException.ThrowIfNull(vertices);

        if (vertices.Count == 0)
            throw new DomainException(SmartGraoErrors.Geo.EmptyBoundary);

        var coordinates = vertices.Select(v => new Coordinate(v.Longitude, v.Latitude)).ToList();

        if (!coordinates[0].Equals2D(coordinates[^1]))
            coordinates.Add(coordinates[0].Copy());

        if (coordinates.Count < MinimumPositions)
            throw new DomainException(SmartGraoErrors.Geo.InsufficientVertices);

        if (coordinates.Count > MaximumPositions)
            throw new DomainException(SmartGraoErrors.Geo.TooManyVertices);

        var ring = Factory.CreateLinearRing([.. coordinates]);
        return FromPolygon(Factory.CreatePolygon(ring));
    }

    /// <summary>
    /// Valida e mede um poligono ja construido. E por aqui que passa a geometria vinda do banco.
    /// </summary>
    public static Boundary FromPolygon(Polygon polygon)
    {
        ArgumentNullException.ThrowIfNull(polygon);

        if (polygon.IsEmpty)
            throw new DomainException(SmartGraoErrors.Geo.EmptyBoundary);

        // Auto-interseccao: o "no de gravata" que aparece quando o produtor cruza a propria linha
        // ao desenhar. O PostGIS aceitaria gravar, e a area sairia sem sentido.
        if (!polygon.IsValid)
            throw new DomainException(SmartGraoErrors.Geo.InvalidPolygon);

        if (polygon.NumInteriorRings > 0)
            throw new DomainException(SmartGraoErrors.Geo.PolygonHasHoles);

        var coordinates = polygon.ExteriorRing.Coordinates;

        if (coordinates.Length < MinimumPositions)
            throw new DomainException(SmartGraoErrors.Geo.InsufficientVertices);

        if (coordinates.Length > MaximumPositions)
            throw new DomainException(SmartGraoErrors.Geo.TooManyVertices);

        // Revalida cada vertice: um poligono construido fora daqui (banco, importacao) nao passou
        // necessariamente pelo construtor de GeoCoordinate.
        foreach (var coordinate in coordinates)
            _ = GeoCoordinate.FromCoordinate(coordinate);

        // O SRID vem zerado quando o poligono e montado sem fabrica; sem ele o PostGIS recusa a
        // insercao na coluna geography(Polygon,4326).
        var normalized = polygon.SRID == Srid.Wgs84 ? polygon : (Polygon)Factory.CreateGeometry(polygon);

        var areaSquareMeters = Wgs84Geodesy.AreaInSquareMeters(coordinates);
        var perimeter = Wgs84Geodesy.PerimeterInMeters(coordinates);

        return new Boundary(normalized, Wgs84Geodesy.ToHectares(areaSquareMeters), perimeter);
    }

    /// <summary>
    /// Contorno recuado para dentro por uma distancia em metros — a bordadura que a amostragem de
    /// monitoramento descarta.
    /// <para>
    /// Devolve <c>null</c> quando a erosao nao deixa area util: ou o talhao e estreito demais e
    /// desaparece, ou ele tem forma de ampulheta e a erosao o parte em pedacos soltos. Nos dois
    /// casos nao existe um contorno recuado, e devolver o pedaco maior seria descartar parte do
    /// talhao em silencio — a amostragem sairia enviesada para um lado sem que ninguem pedisse isso.
    /// Nao e violacao de invariante, entao nao lanca: quem chama e que sabe se a ausencia de miolo e
    /// um erro.
    /// </para>
    /// </summary>
    public Boundary? Shrink(double meters)
    {
        if (meters < 0)
            throw new DomainException(SmartGraoErrors.Geo.NegativeShrinkDistance);

        if (meters == 0)
            return this;

        // O recuo e pedido em metros, mas o poligono vive em graus, e um grau de longitude vale
        // menos metros que um grau de latitude. Erodir direto em graus recuaria mais no sentido
        // norte-sul do que no leste-oeste — uma bordadura mais larga em cima e embaixo do que dos
        // lados. Por isso a longitude e comprimida por cos(lat) antes: nesse espaco os dois eixos
        // tem a mesma escala metrica, o recuo sai uniforme, e a compressao e desfeita no fim.
        var latitude = Polygon.Centroid.Coordinate.Y;
        var longitudeScale = Math.Cos(latitude * Math.PI / 180d);

        var compress = AffineTransformation.ScaleInstance(longitudeScale, 1d);
        var expand = AffineTransformation.ScaleInstance(1d / longitudeScale, 1d);

        var eroded = compress.Transform(Polygon)
            .Buffer(-Wgs84Geodesy.MetersToDegreesLatitude(meters));

        if (eroded.IsEmpty || eroded is not Polygon shrunken)
            return null;

        return FromPolygon((Polygon)expand.Transform(shrunken));
    }

    /// <summary>Verdadeiro quando o ponto cai dentro do contorno. Base da amostragem do Pilar 2.</summary>
    public bool Contains(GeoCoordinate coordinate)
    {
        ArgumentNullException.ThrowIfNull(coordinate);
        return Polygon.Contains(coordinate.ToPoint());
    }

    /// <summary>Verdadeiro quando os contornos se sobrepoem em area (encostar na borda nao conta).</summary>
    public bool OverlapsWith(Boundary other)
    {
        ArgumentNullException.ThrowIfNull(other);

        // Intersects incluiria talhoes que apenas dividem a divisa, que e o caso normal e nao um
        // conflito. A matriz "T********" exige interseccao entre os interiores: area em comum de
        // verdade.
        return Polygon.Relate(other.Polygon, "T********");
    }

    public bool Equals(Boundary? other) => other is not null && Polygon.EqualsTopologically(other.Polygon);

    public override bool Equals(object? obj) => obj is Boundary boundary && Equals(boundary);

    public override int GetHashCode() => Polygon.EnvelopeInternal.GetHashCode();
}
