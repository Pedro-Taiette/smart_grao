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

    /// <summary>Quadrado de aproximadamente 1000 m de lado, corrigindo a longitude pela latitude.</summary>
    internal static GeoCoordinate[] OneKilometreSquare(double latitude, double longitude)
    {
        var degreesPerMetreInLatitude = 180d / (Math.PI * Wgs84Geodesy.AuthalicRadiusMeters);
        var deltaLatitude = 1_000d * degreesPerMetreInLatitude;
        var deltaLongitude = deltaLatitude / Math.Cos(latitude * Math.PI / 180d);

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
