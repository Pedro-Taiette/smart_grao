using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Geo;

public sealed class BoundaryTests
{
    [Fact]
    public void OpenRing_IsClosedAutomatically()
    {
        // O Leaflet-Geoman entrega o contorno sem repetir o primeiro ponto.
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));

        Assert.True(boundary.Polygon.IsValid);
        Assert.Equal(4, boundary.VertexCount);
        Assert.Equal(5, boundary.Polygon.ExteriorRing.NumPoints);
    }

    [Fact]
    public void SelfIntersectingPolygon_IsRejected()
    {
        // O "no de gravata": acontece quando o produtor cruza a propria linha ao desenhar.
        var bowtie = new[]
        {
            GeoCoordinate.From(-15.60, -56.10),
            GeoCoordinate.From(-15.59, -56.09),
            GeoCoordinate.From(-15.60, -56.09),
            GeoCoordinate.From(-15.59, -56.10),
        };

        var exception = Assert.Throws<DomainException>(() => Boundary.FromCoordinates(bowtie));

        Assert.Equal("geo.invalid_polygon", exception.Error.Code);
    }

    [Fact]
    public void FewerThanThreeVertices_IsRejected()
    {
        var exception = Assert.Throws<DomainException>(() => Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.60, -56.10),
            GeoCoordinate.From(-15.59, -56.09),
        ]));

        Assert.Equal("geo.insufficient_vertices", exception.Error.Code);
    }

    [Theory]
    [InlineData(91, 0, "geo.latitude_out_of_range")]
    [InlineData(0, 181, "geo.longitude_out_of_range")]
    [InlineData(double.NaN, 0, "geo.coordinate_not_finite")]
    public void OutOfRangeCoordinate_IsRejected(double latitude, double longitude, string expectedCode)
    {
        var exception = Assert.Throws<DomainException>(() => GeoCoordinate.From(latitude, longitude));

        Assert.Equal(expectedCode, exception.Error.Code);
    }

    [Fact]
    public void Contains_TellsInsideFromOutside()
    {
        var boundary = Boundary.FromCoordinates(Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1));
        var center = boundary.Center;

        Assert.True(boundary.Contains(center));
        Assert.False(boundary.Contains(GeoCoordinate.From(center.Latitude + 1, center.Longitude)));
    }

    /// <summary>
    /// Dois talhoes que dividem a divisa sao o caso normal, nao um conflito. Se isso contasse como
    /// sobreposicao, nenhuma fazenda conseguiria cadastrar o segundo talhao.
    /// </summary>
    [Fact]
    public void NeighbouringFields_SharingOnlyAnEdge_DoNotOverlap()
    {
        var west = Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.60, -56.10),
            GeoCoordinate.From(-15.60, -56.09),
            GeoCoordinate.From(-15.59, -56.09),
            GeoCoordinate.From(-15.59, -56.10),
        ]);

        var east = Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.60, -56.09),
            GeoCoordinate.From(-15.60, -56.08),
            GeoCoordinate.From(-15.59, -56.08),
            GeoCoordinate.From(-15.59, -56.09),
        ]);

        Assert.True(west.Polygon.Intersects(east.Polygon));
        Assert.False(west.OverlapsWith(east));
    }

    [Fact]
    public void FieldsSharingArea_DoOverlap()
    {
        var first = Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.60, -56.10),
            GeoCoordinate.From(-15.60, -56.09),
            GeoCoordinate.From(-15.59, -56.09),
            GeoCoordinate.From(-15.59, -56.10),
        ]);

        var intruder = Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.595, -56.095),
            GeoCoordinate.From(-15.595, -56.085),
            GeoCoordinate.From(-15.585, -56.085),
            GeoCoordinate.From(-15.585, -56.095),
        ]);

        Assert.True(first.OverlapsWith(intruder));
        Assert.True(intruder.OverlapsWith(first));
    }

    [Fact]
    public void Vertices_ExcludeTheRepeatedClosingPoint()
    {
        var original = Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1);
        var boundary = Boundary.FromCoordinates(original);

        Assert.Equal(original.Length, boundary.Vertices.Count);
        Assert.Equal(original[0].Latitude, boundary.Vertices[0].Latitude, precision: 9);
    }
}
