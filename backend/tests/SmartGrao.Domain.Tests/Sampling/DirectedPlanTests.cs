using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling;
using Xunit;

namespace SmartGrao.Domain.Tests.Sampling;

public sealed class DirectedPlanTests
{
    private static readonly FieldId Field = FieldId.New();
    private static readonly CultivationId Cultivation = CultivationId.New();
    private static readonly DateOnly PassDate = new(2026, 4, 5);

    private static Boundary Square(double latitude, double longitude, double hectares)
    {
        var side = Math.Sqrt(hectares * Wgs84Geodesy.SquareMetersPerHectare);
        var deltaLatitude = Wgs84Geodesy.MetersToDegreesLatitude(side);
        var deltaLongitude = Wgs84Geodesy.MetersToDegreesLongitude(side, latitude);
        return Boundary.FromCoordinates(
        [
            GeoCoordinate.From(latitude, longitude),
            GeoCoordinate.From(latitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude + deltaLongitude),
            GeoCoordinate.From(latitude + deltaLatitude, longitude),
        ]);
    }

    private static readonly Boundary FieldOutline = Square(-15.6, -56.1, 100);

    [Fact]
    public void WithoutZonesTheTenStopsCoverTheWholeField()
    {
        var plan = SamplingPlan.Directed(Field, FieldOutline, [], PassDate, Cultivation);

        Assert.Equal(SamplingMode.Directed, plan.Mode);
        Assert.Equal(SamplingPlan.DirectedStops, plan.PointCount);
        Assert.Null(plan.ZoneDate);
        Assert.All(plan.Points, point => Assert.True(FieldOutline.Contains(point.Coordinate)));
    }

    [Fact]
    public void StopsFallInsideTheZones()
    {
        var zones = new[] { Square(-15.595, -56.095, 6), Square(-15.592, -56.092, 2) };

        var plan = SamplingPlan.Directed(Field, FieldOutline, zones, PassDate, Cultivation);

        Assert.Equal(PassDate, plan.ZoneDate);
        Assert.InRange(plan.PointCount, 8, SamplingPlan.DirectedStops);
        Assert.All(plan.Points, point => Assert.Contains(zones, zone => zone.Contains(point.Coordinate)));
    }

    [Fact]
    public void EveryZoneGetsAtLeastOneStop()
    {
        var zones = new[] { Square(-15.595, -56.095, 20), Square(-15.592, -56.092, 0.5) };

        var plan = SamplingPlan.Directed(Field, FieldOutline, zones, PassDate, Cultivation);

        Assert.Contains(plan.Points, point => zones[1].Contains(point.Coordinate));
    }
}
