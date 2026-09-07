using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Farms;

public sealed class FarmTests
{
    [Fact]
    public void Create_NormalisesTheStateCode()
    {
        var farm = Farm.Create("Fazenda Santa Rita", "Sorriso", "mt");

        Assert.Equal("MT", farm.State);
    }

    [Theory]
    [InlineData("M")]
    [InlineData("MTO")]
    [InlineData(" ")]
    public void Create_WithAnInvalidStateCode_IsRejected(string state)
    {
        var exception = Assert.Throws<DomainException>(
            () => Farm.Create("Fazenda Santa Rita", "Sorriso", state));

        Assert.Equal("farm.invalid_state", exception.Error.Code);
    }

    [Fact]
    public void Headquarters_WhenProvided_RoundTripsAsACoordinate()
    {
        var headquarters = GeoCoordinate.From(-12.5453, -55.7211);
        var farm = Farm.Create("Fazenda Santa Rita", "Sorriso", "MT", headquarters);

        Assert.NotNull(farm.Headquarters);
        Assert.Equal(Srid.Wgs84, farm.Headquarters!.SRID);

        // Em NTS, X e longitude e Y e latitude. Trocar os dois joga a fazenda no oceano.
        Assert.Equal(-55.7211, farm.Headquarters.X, precision: 6);
        Assert.Equal(-12.5453, farm.Headquarters.Y, precision: 6);
        Assert.Equal(headquarters, farm.HeadquartersCoordinate);
    }

    [Fact]
    public void Headquarters_IsOptional()
    {
        var farm = Farm.Create("Fazenda Santa Rita", "Sorriso", "MT");

        Assert.Null(farm.Headquarters);
        Assert.Null(farm.HeadquartersCoordinate);
    }
}
