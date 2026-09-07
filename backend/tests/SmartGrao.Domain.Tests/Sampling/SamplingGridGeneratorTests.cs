using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling;
using SmartGrao.Domain.Tests.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Sampling;

/// <summary>
/// A malha e o produto do Pilar 2: e ela que o agronomo caminha e onde cada foto de diagnostico vai
/// ser tirada. Um ponto fora do talhao manda alguem para a lavoura do vizinho; uma malha torta mede
/// metade da area e chama isso de media.
/// </summary>
public sealed class SamplingGridGeneratorTests
{
    /// <summary>
    /// Quadrado de 1 km com espacamento de 100 m: 10 celulas por lado, um ponto no centro de cada
    /// uma, 100 no total.
    /// <para>
    /// Este numero e a razao de os pontos ficarem no centro da celula e nao no canto. Ancorados nos
    /// cantos seriam 11x11 = 121 candidatos, mas a fileira externa cairia exatamente sobre a divisa
    /// e o <c>Contains</c> exclui a fronteira: sobrariam 99, com o anel externo perdido de um jeito
    /// que depende de arredondamento de ponto flutuante.
    /// </para>
    /// </summary>
    [Fact]
    public void KilometreSquare_AtOneHundredMetres_FillsTheExpectedGrid()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Standard);

        Assert.Equal(100, points.Count);
    }

    /// <summary>
    /// Espacamento menor tem que render mais pontos, na proporcao do quadrado da razao. E a
    /// checagem de que o passo realmente entra na conta, e nao so o contorno.
    /// </summary>
    [Fact]
    public void SmallerSpacing_ProducesADenserGrid()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var standard = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Standard).Count;
        var detailed = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Detailed).Count;

        Assert.True(detailed > standard * 3, $"esperava ~4x mais pontos, veio {detailed} contra {standard}");
    }

    /// <summary>Todo ponto devolvido cai dentro do contorno. E a invariante que nao pode falhar.</summary>
    [Fact]
    public void EveryPoint_FallsInsideTheBoundary()
    {
        var boundary = Boundary.FromCoordinates(LShapedField());

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Standard);

        Assert.NotEmpty(points);
        Assert.All(points, point => Assert.True(boundary.Contains(point)));
    }

    /// <summary>
    /// Talhao em L: o quadrante vazio da caixa envolvente nao pode receber ponto nenhum. E o teste
    /// que separa "gerou a grade sobre a caixa" de "recortou pelo contorno" — sem o recorte, a
    /// contagem seria a do retangulo inteiro.
    /// </summary>
    [Fact]
    public void LShapedField_LeavesTheEmptyQuadrantEmpty()
    {
        var boundary = Boundary.FromCoordinates(LShapedField());
        var box = boundary.BoundingBox;

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Detailed);

        // O recorte do L removeu o quadrante nordeste; nenhum ponto pode estar la.
        var emptyQuadrant = points.Where(point =>
            point.Latitude > (box.MinY + box.MaxY) / 2d &&
            point.Longitude > (box.MinX + box.MaxX) / 2d);

        Assert.Empty(emptyQuadrant);
        Assert.NotEmpty(points);
    }

    /// <summary>
    /// A grade fica centrada na caixa envolvente: a folga sobrando da divisao e repartida entre as
    /// duas pontas. Sem isso a malha encosta no sul e no oeste e deixa uma faixa sem amostra no
    /// norte e no leste — o talhao sairia amostrado torto.
    /// </summary>
    [Fact]
    public void Grid_IsCentredInTheBoundingBox()
    {
        // Espacamento que nao divide o lado de forma exata, para que sobre folga de verdade.
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));
        var box = boundary.BoundingBox;

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Intermediate);

        var marginSouth = points.Min(p => p.Latitude) - box.MinY;
        var marginNorth = box.MaxY - points.Max(p => p.Latitude);

        Assert.Equal(marginSouth, marginNorth, tolerance: marginSouth * 0.05);
    }

    /// <summary>
    /// Ordem em serpentina: o passo entre pontos consecutivos nunca atravessa o talhao inteiro. Na
    /// ordem ingenua — toda fileira da esquerda para a direita — o fim de cada linha saltaria de
    /// volta ao outro extremo, e o caminho gerado seria impraticavel de caminhar.
    /// </summary>
    [Fact]
    public void ConsecutivePoints_NeverJumpAcrossTheField()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Standard);

        // Uma diagonal de fileira vale ~100 m de passo lateral mais ~100 m de subida; o salto de
        // volta valeria ~1000 m. O corte em 300 m separa os dois casos com folga.
        var longestStep = points
            .Zip(points.Skip(1), (from, to) => from.DistanceInMetersTo(to))
            .Max();

        Assert.True(longestStep < 300d, $"maior passo foi {longestStep:F0} m");
    }

    /// <summary>
    /// Talhao menor que o espacamento ainda rende o ponto central, em vez de lista vazia. E o caso
    /// do talhao de 1 ha no modo Mapeamento com grade de 100 m.
    /// </summary>
    [Fact]
    public void FieldSmallerThanTheSpacing_StillYieldsItsCentre()
    {
        var boundary = Boundary.FromCoordinates(SquareOfSide(-15.6, -56.1, 120d));

        var points = SamplingGridGenerator.Generate(boundary, SamplingSpacing.Standard);

        Assert.Single(points);
        Assert.True(boundary.Contains(points[0]));
    }

    /// <summary>
    /// Grade absurdamente densa e recusada antes de gerar. Sem o teto, um talhao grande com
    /// espacamento pequeno pediria milhoes de testes de contencao e prenderia a requisicao.
    /// </summary>
    [Fact]
    public void AbsurdlyDenseGrid_IsRefusedBeforeItIsBuilt()
    {
        var boundary = Boundary.FromCoordinates(SquareOfSide(-15.6, -56.1, 20_000d));

        var exception = Assert.Throws<DomainException>(
            () => SamplingGridGenerator.Generate(boundary, SamplingSpacing.Detailed));

        Assert.Equal("sampling.grid_too_dense", exception.Error.Code);
    }

    /// <summary>Quadrado de lado aproximado em metros.</summary>
    internal static GeoCoordinate[] SquareOfSide(double latitude, double longitude, double sideMetres)
    {
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(sideMetres);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(sideMetres, latitude);

        return
        [
            GeoCoordinate.From(latitude, longitude),
            GeoCoordinate.From(latitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude),
        ];
    }

    /// <summary>
    /// Talhao em L de ~1 km: o quadrante nordeste da caixa envolvente fica de fora do contorno.
    /// </summary>
    private static GeoCoordinate[] LShapedField()
    {
        const double Latitude = -15.6;
        const double Longitude = -56.1;

        var full = Wgs84Geodesy.MetersToDegreesLatitude(1_000d);
        var half = full / 2d;
        var fullLongitude = Wgs84Geodesy.MetersToDegreesLongitude(1_000d, Latitude);
        var halfLongitude = fullLongitude / 2d;

        return
        [
            GeoCoordinate.From(Latitude, Longitude),
            GeoCoordinate.From(Latitude, Longitude + fullLongitude),
            GeoCoordinate.From(Latitude + half, Longitude + fullLongitude),
            GeoCoordinate.From(Latitude + half, Longitude + halfLongitude),
            GeoCoordinate.From(Latitude + full, Longitude + halfLongitude),
            GeoCoordinate.From(Latitude + full, Longitude),
        ];
    }
}
