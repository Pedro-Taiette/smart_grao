using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Geo;

/// <summary>
/// Medidas sobre a superficie da Terra a partir de coordenadas em graus.
/// <para>
/// Existe porque a area de um poligono em WGS84 <b>nao</b> e a area planar que o NetTopologySuite
/// devolve: <c>Polygon.Area</c> soma graus quadrados, unidade que nao converte para hectare porque
/// um grau de longitude vale ~111 km na linha do Equador e ~85 km no Rio Grande do Sul. Um talhao
/// de 40 ha medido pelo caminho errado erra por dezenas de hectares — o suficiente para dimensionar
/// a pulverizacao errada.
/// </para>
/// <para>
/// O modelo aqui e a esfera autalica do WGS84 (raio de igual area). Contra o elipsoide o erro fica
/// abaixo de ~0,1% em talhoes agricolas, ordens de grandeza melhor do que a incerteza do proprio
/// tracado feito com o dedo sobre o mapa. E a mesma familia de formula que o PostGIS aplica em
/// <c>geography</c>, entao o valor calculado aqui e o que o banco confirmaria.
/// </para>
/// </summary>
public static class Wgs84Geodesy
{
    /// <summary>Raio autalico do WGS84, em metros: a esfera com a mesma area do elipsoide.</summary>
    public const double AuthalicRadiusMeters = 6_371_007.181;

    public const double SquareMetersPerHectare = 10_000d;

    /// <summary>
    /// Acima disso a conversao de metros para graus de longitude divide por um cosseno proximo de
    /// zero e explode. Nao e regra agronomica — nenhum talhao existe a 89 graus de latitude —, e sim
    /// a recusa de devolver <c>Infinity</c> em silencio para quem for montar uma malha.
    /// </summary>
    public const double MaximumLatitudeForMetricConversion = 89d;

    /// <summary>
    /// Quantos metros vale um grau de latitude. Constante em qualquer ponto do globo: os meridianos
    /// sao circulos maximos.
    /// </summary>
    public static double MetersPerDegreeLatitude => Math.PI * AuthalicRadiusMeters / 180d;

    /// <summary>
    /// Quantos metros vale um grau de longitude na latitude dada. Encolhe com o cosseno da latitude
    /// — os paralelos vao ficando menores em direcao aos polos —, e e por isso que uma malha de
    /// espacamento fixo em metros nao pode usar o mesmo passo nos dois eixos.
    /// </summary>
    public static double MetersPerDegreeLongitude(double atLatitude) =>
        MetersPerDegreeLatitude * Math.Cos(ToRadians(GuardLatitude(atLatitude)));

    /// <summary>Converte uma distancia em metros para o deslocamento equivalente em graus de latitude.</summary>
    public static double MetersToDegreesLatitude(double meters) => meters / MetersPerDegreeLatitude;

    /// <summary>
    /// Converte uma distancia em metros para o deslocamento equivalente em graus de longitude, na
    /// latitude dada.
    /// </summary>
    public static double MetersToDegreesLongitude(double meters, double atLatitude) =>
        meters / MetersPerDegreeLongitude(atLatitude);

    /// <summary>
    /// Area geodesica do anel, em metros quadrados, pela formula do excesso esferico
    /// (Chamberlain &amp; Duquette). O valor absoluto e tomado no fim porque o sinal so informa a
    /// orientacao do anel (horario ou anti-horario), e o GeoJSON nao garante nenhuma das duas.
    /// </summary>
    public static double AreaInSquareMeters(IReadOnlyList<Coordinate> ring)
    {
        ArgumentNullException.ThrowIfNull(ring);

        if (ring.Count < 4)
            return 0d;

        var sum = 0d;

        // O anel e fechado (primeiro ponto == ultimo), entao a ultima aresta ja esta incluida e o
        // laco para em Count - 1 para nao conta-la duas vezes.
        for (var i = 0; i < ring.Count - 1; i++)
        {
            var current = ring[i];
            var next = ring[i + 1];

            var lambda1 = ToRadians(current.X);
            var lambda2 = ToRadians(next.X);
            var phi1 = ToRadians(current.Y);
            var phi2 = ToRadians(next.Y);

            sum += (lambda2 - lambda1) * (Math.Sin(phi1) + Math.Sin(phi2));
        }

        return Math.Abs(sum * AuthalicRadiusMeters * AuthalicRadiusMeters / 2d);
    }

    /// <summary>Perimetro geodesico do anel, em metros.</summary>
    public static double PerimeterInMeters(IReadOnlyList<Coordinate> ring)
    {
        ArgumentNullException.ThrowIfNull(ring);

        var total = 0d;
        for (var i = 0; i < ring.Count - 1; i++)
            total += DistanceInMeters(ring[i], ring[i + 1]);

        return total;
    }

    public static double DistanceInMeters(GeoCoordinate origin, GeoCoordinate destination)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        return Haversine(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
    }

    public static double DistanceInMeters(Coordinate origin, Coordinate destination)
    {
        ArgumentNullException.ThrowIfNull(origin);
        ArgumentNullException.ThrowIfNull(destination);

        return Haversine(origin.Y, origin.X, destination.Y, destination.X);
    }

    /// <summary>Metros quadrados para hectares, arredondado a 4 casas (1 m² = 0,0001 ha).</summary>
    public static decimal ToHectares(double squareMeters) =>
        Math.Round((decimal)(squareMeters / SquareMetersPerHectare), 4, MidpointRounding.AwayFromZero);

    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        var deltaPhi = ToRadians(lat2 - lat1);
        var deltaLambda = ToRadians(lon2 - lon1);
        var phi1 = ToRadians(lat1);
        var phi2 = ToRadians(lat2);

        var a = (Math.Sin(deltaPhi / 2) * Math.Sin(deltaPhi / 2)) +
                (Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(deltaLambda / 2) * Math.Sin(deltaLambda / 2));

        return 2 * AuthalicRadiusMeters * Math.Asin(Math.Min(1d, Math.Sqrt(a)));
    }

    private static double GuardLatitude(double latitude)
    {
        if (double.IsNaN(latitude) || double.IsInfinity(latitude))
            throw new DomainException(SmartGraoErrors.Geo.CoordinateNotFinite);

        if (Math.Abs(latitude) >= MaximumLatitudeForMetricConversion)
            throw new DomainException(SmartGraoErrors.Geo.LatitudeTooCloseToPole);

        return latitude;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
