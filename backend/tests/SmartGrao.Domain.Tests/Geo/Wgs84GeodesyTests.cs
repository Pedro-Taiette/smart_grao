using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Geo;

/// <summary>
/// A area do talhao e o numero que sai deste arquivo. Se ele estiver errado, todo hectare exibido
/// na tela, toda dose de defensivo calculada e toda densidade de amostragem estao errados junto —
/// e de um jeito que ninguem percebe, porque um numero plausivel nao levanta suspeita.
/// </summary>
public sealed class Wgs84GeodesyTests
{
    /// <summary>
    /// Confere a soma sobre os vertices contra a forma fechada da area de uma zona esferica,
    /// <c>R² · Δλ · (sen φ₂ − sen φ₁)</c>, calculada aqui de forma independente. Nao e o mesmo
    /// caminho de codigo: um erro de sinal, de fator ou de conversao de grau para radiano na formula
    /// do excesso esferico apareceria como divergencia.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.01)]      // sobre o Equador
    [InlineData(-15.6, 0.01)]    // Mato Grosso
    [InlineData(-28.2, 0.05)]    // Rio Grande do Sul, retangulo maior
    public void RectangleArea_MatchesTheSphericalZoneFormula(double baseLatitude, double side)
    {
        var vertices = new[]
        {
            GeoCoordinate.From(baseLatitude, 0),
            GeoCoordinate.From(baseLatitude, side),
            GeoCoordinate.From(baseLatitude + side, side),
            GeoCoordinate.From(baseLatitude + side, 0),
        };

        var expected = SphericalZoneArea(baseLatitude, baseLatitude + side, side);
        var actual = Boundary.FromCoordinates(vertices).AreaHectares;

        Assert.Equal((double)Wgs84Geodesy.ToHectares(expected), (double)actual, precision: 2);
    }

    /// <summary>Um quadrado de um quilometro de lado tem 100 hectares. E a checagem de bom senso.</summary>
    [Fact]
    public void OneKilometreSquare_IsOneHundredHectares()
    {
        var boundary = Boundary.FromCoordinates(OneKilometreSquare(-15.6, -56.1));

        Assert.InRange(boundary.AreaHectares, 99.5m, 100.5m);
    }

    [Fact]
    public void OneKilometreSquare_HasAFourKilometrePerimeter()
    {
        var boundary = Boundary.FromCoordinates(OneKilometreSquare(-15.6, -56.1));

        Assert.InRange(boundary.PerimeterMeters, 3_980d, 4_020d);
    }

    /// <summary>
    /// O sentido do tracado nao muda a area. Importa porque o GeoJSON nao garante orientacao e o
    /// Leaflet-Geoman entrega o que o dedo do produtor desenhou — sem o valor absoluto, metade dos
    /// talhoes teria area negativa e cairia abaixo do minimo.
    /// </summary>
    [Fact]
    public void RingOrientation_DoesNotChangeTheArea()
    {
        var clockwise = OneKilometreSquare(-15.6, -56.1);
        var counterClockwise = clockwise.Reverse().ToArray();

        Assert.Equal(
            Boundary.FromCoordinates(clockwise).AreaHectares,
            Boundary.FromCoordinates(counterClockwise).AreaHectares);
    }

    /// <summary>
    /// A area planar do NetTopologySuite, em graus quadrados, nao e a area geodesica. O teste fixa
    /// essa diferenca por escrito para que ninguem "simplifique" o calculo trocando um pelo outro:
    /// o numero planar aqui e menor que a area real por um fator de dez milhoes.
    /// </summary>
    [Fact]
    public void NtsPlanarArea_IsNotAValidFieldMeasure()
    {
        var boundary = Boundary.FromCoordinates(OneKilometreSquare(-15.6, -56.1));

        var planarInSquareDegrees = boundary.Polygon.Area;
        var geodesicInSquareMeters = (double)boundary.AreaHectares * Wgs84Geodesy.SquareMetersPerHectare;

        Assert.True(planarInSquareDegrees < 1e-3);
        Assert.InRange(geodesicInSquareMeters, 995_000d, 1_005_000d);
    }

    /// <summary>
    /// Um grau de latitude vale o mesmo em qualquer lugar, e a ida e volta tem que fechar. Sem isso
    /// a malha amostral nasce com o passo errado e ninguem nota: os pontos continuam dentro do
    /// talhao, so que espacados de outra coisa que nao o que foi pedido.
    /// </summary>
    [Theory]
    [InlineData(50d)]
    [InlineData(100d)]
    [InlineData(1_000d)]
    public void LatitudeConversion_RoundTrips(double meters)
    {
        var degrees = Wgs84Geodesy.MetersToDegreesLatitude(meters);

        Assert.Equal(meters, degrees * Wgs84Geodesy.MetersPerDegreeLatitude, precision: 6);
    }

    /// <summary>
    /// Um grau de longitude encolhe com o cosseno da latitude. E a razao de a malha precisar de dois
    /// passos diferentes: usar o passo da latitude tambem na longitude esticaria a grade no sentido
    /// leste-oeste, e no Rio Grande do Sul o erro passa de 10%.
    /// </summary>
    [Theory]
    [InlineData(0d)]
    [InlineData(-15.6)]
    [InlineData(-28.2)]
    public void LongitudeConversion_RoundTripsAtItsOwnLatitude(double latitude)
    {
        var degrees = Wgs84Geodesy.MetersToDegreesLongitude(100d, latitude);

        Assert.Equal(100d, degrees * Wgs84Geodesy.MetersPerDegreeLongitude(latitude), precision: 6);
    }

    /// <summary>
    /// A conversao de longitude confere contra a distancia geodesica medida de verdade: cem metros
    /// convertidos para graus e reconvertidos por Haversine tem que dar cem metros. Nao e o mesmo
    /// caminho de codigo, entao um fator errado apareceria aqui.
    /// </summary>
    [Theory]
    [InlineData(0d)]
    [InlineData(-15.6)]
    [InlineData(-28.2)]
    public void LongitudeConversion_MatchesTheMeasuredDistance(double latitude)
    {
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(100d, latitude);

        var measured = Wgs84Geodesy.DistanceInMeters(
            GeoCoordinate.From(latitude, 0),
            GeoCoordinate.From(latitude, deltaLongitude));

        Assert.Equal(100d, measured, precision: 3);
    }

    /// <summary>
    /// No Equador o grau de longitude vale o mesmo que o de latitude; a 60 graus, metade. Fixa a
    /// direcao do encolhimento por escrito — trocar o cosseno pelo seno passaria nos testes de ida e
    /// volta acima, porque eles usam a mesma funcao nos dois sentidos.
    /// </summary>
    [Fact]
    public void LongitudeDegree_ShrinksTowardsThePoles()
    {
        Assert.Equal(
            Wgs84Geodesy.MetersPerDegreeLatitude,
            Wgs84Geodesy.MetersPerDegreeLongitude(0d),
            precision: 6);

        Assert.Equal(
            Wgs84Geodesy.MetersPerDegreeLatitude / 2d,
            Wgs84Geodesy.MetersPerDegreeLongitude(60d),
            precision: 6);
    }

    /// <summary>
    /// Perto do polo a divisao pelo cosseno explode. Devolver <c>Infinity</c> em silencio faria a
    /// malha nascer vazia ou o laco nao terminar, entao a conversao recusa.
    /// </summary>
    [Theory]
    [InlineData(89.5)]
    [InlineData(-90d)]
    public void MetricConversion_RefusesLatitudesNearThePoles(double latitude)
    {
        var exception = Assert.Throws<DomainException>(
            () => Wgs84Geodesy.MetersToDegreesLongitude(100d, latitude));

        Assert.Equal("geo.latitude_too_close_to_pole", exception.Error.Code);
    }

    /// <summary>Quadrado de aproximadamente 1000 m de lado, corrigindo a longitude pela latitude.</summary>
    internal static GeoCoordinate[] OneKilometreSquare(double latitude, double longitude)
    {
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(1_000d);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(1_000d, latitude);

        return
        [
            GeoCoordinate.From(latitude, longitude),
            GeoCoordinate.From(latitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude),
        ];
    }

    private static double SphericalZoneArea(double lowerLatitude, double upperLatitude, double deltaLongitude)
    {
        var radiusSquared = Wgs84Geodesy.AuthalicRadiusMeters * Wgs84Geodesy.AuthalicRadiusMeters;
        var deltaLambda = deltaLongitude * Math.PI / 180d;

        var upperSine = Math.Sin(upperLatitude * Math.PI / 180d);
        var lowerSine = Math.Sin(lowerLatitude * Math.PI / 180d);

        return radiusSquared * deltaLambda * (upperSine - lowerSine);
    }
}
