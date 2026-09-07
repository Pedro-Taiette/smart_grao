using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Geo;

/// <summary>
/// A bordadura descartada pelo modo Monitoramento nasce aqui. O que esta em jogo nao e geometria
/// bonita: amostrar na borda mede uma populacao que a fonte da Embrapa registra como uma ordem de
/// grandeza maior que a do interior, e uma faixa recuada errado devolve a media do talhao
/// contaminada — com aplicacao de defensivo em area que nao precisava.
/// </summary>
public sealed class BoundaryShrinkTests
{
    [Fact]
    public void ZeroDistance_ReturnsTheSameBoundary()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        Assert.Same(boundary, boundary.Shrink(0));
    }

    [Fact]
    public void NegativeDistance_IsRejected()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var exception = Assert.Throws<DomainException>(() => boundary.Shrink(-10));

        Assert.Equal("geo.negative_shrink_distance", exception.Error.Code);
    }

    /// <summary>
    /// Um quadrado de 1 km recuado em 100 m vira um quadrado de 800 m — 64 ha. Confere a magnitude
    /// do recuo pela area, que e independente do caminho de codigo que produziu o poligono.
    /// </summary>
    [Fact]
    public void ShrinkingAKilometreSquare_LeavesTheExpectedArea()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var shrunken = boundary.Shrink(100);

        Assert.NotNull(shrunken);
        Assert.InRange(shrunken.AreaHectares, 63.0m, 65.0m);
    }

    /// <summary>
    /// O teste que justifica a compressao por cos(lat) dentro do <c>Shrink</c>. Erodindo direto em
    /// graus, o recuo sairia maior no sentido norte-sul do que no leste-oeste, e o retangulo
    /// resultante deixaria de ser quadrado. Medindo os dois lados em metros, eles tem que continuar
    /// iguais entre si.
    /// <para>
    /// A 28 graus sul, <c>cos(lat)</c> vale 0,88: a versao ingenua recuaria 12% menos no sentido
    /// leste-oeste, o que num quadrado de 1 km recuado em 100 m aparece como ~3% de diferenca entre
    /// os lados. A tolerancia de 1% fica abaixo disso, entao este teste reprova a versao ingenua.
    /// </para>
    /// </summary>
    [Fact]
    public void ShrinkIsUniformInMetres_NotInDegrees()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-28.2, -53.8));

        var shrunken = boundary.Shrink(100);

        Assert.NotNull(shrunken);

        var box = shrunken.BoundingBox;
        var centreLatitude = (box.MinY + box.MaxY) / 2d;

        var widthInMetres = Wgs84Geodesy.DistanceInMeters(
            GeoCoordinate.From(centreLatitude, box.MinX),
            GeoCoordinate.From(centreLatitude, box.MaxX));

        var heightInMetres = Wgs84Geodesy.DistanceInMeters(
            GeoCoordinate.From(box.MinY, box.MinX),
            GeoCoordinate.From(box.MaxY, box.MinX));

        Assert.Equal(widthInMetres, heightInMetres, tolerance: widthInMetres * 0.01);
    }

    /// <summary>
    /// Recuo maior que o miolo do talhao: nao sobra area. Devolve null em vez de um poligono vazio,
    /// porque poligono vazio passaria adiante e so quebraria mais tarde, na geracao da malha.
    /// </summary>
    [Fact]
    public void ShrinkingMoreThanTheFieldHolds_ReturnsNull()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        Assert.Null(boundary.Shrink(600));
    }

    /// <summary>
    /// Talhao em ampulheta: a erosao parte o contorno em dois lobos soltos. Devolver o lobo maior
    /// seria descartar metade do talhao sem avisar, entao aqui tambem e null.
    /// </summary>
    [Fact]
    public void ShrinkingAnHourglass_ReturnsNullInsteadOfTheLargestLobe()
    {
        // Dois blocos de ~1 km ligados por um istmo de ~110 m: estreito o bastante para que um
        // recuo de 120 m de cada lado o consuma e separe os lobos.
        var hourglass = new[]
        {
            GeoCoordinate.From(-15.6000, -56.1000),
            GeoCoordinate.From(-15.6000, -56.0900),
            GeoCoordinate.From(-15.5955, -56.0945),
            GeoCoordinate.From(-15.5910, -56.0900),
            GeoCoordinate.From(-15.5910, -56.1000),
            GeoCoordinate.From(-15.5955, -56.0955),
        };

        var boundary = Boundary.FromCoordinates(hourglass);

        Assert.Null(boundary.Shrink(120));
    }

    /// <summary>
    /// O contorno recuado continua sendo um <see cref="Boundary"/> de verdade — valido, medido e
    /// menor que o original. Sem isso ele nao serviria de entrada para o recorte da malha.
    /// </summary>
    [Fact]
    public void ShrunkenBoundary_IsAValidMeasuredBoundary()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var shrunken = boundary.Shrink(50);

        Assert.NotNull(shrunken);
        Assert.True(shrunken.Polygon.IsValid);
        Assert.Equal(Srid.Wgs84, shrunken.Polygon.SRID);
        Assert.True(shrunken.AreaHectares < boundary.AreaHectares);
        Assert.True(shrunken.PerimeterMeters < boundary.PerimeterMeters);
    }

    /// <summary>
    /// Todo ponto do contorno recuado esta dentro do original. E a garantia que a amostragem usa:
    /// os pontos da malha nascem dentro do talhao, nao apenas perto dele.
    /// </summary>
    [Fact]
    public void ShrunkenBoundary_LiesInsideTheOriginal()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        var shrunken = boundary.Shrink(100);

        Assert.NotNull(shrunken);
        Assert.All(shrunken.Vertices, vertex => Assert.True(boundary.Contains(vertex)));
    }
}
