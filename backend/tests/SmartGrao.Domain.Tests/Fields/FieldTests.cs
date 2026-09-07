using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Fields.Events;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Tests.Geo;
using Xunit;

namespace SmartGrao.Domain.Tests.Fields;

public sealed class FieldTests
{
    private static readonly FarmId AFarm = FarmId.New();

    [Fact]
    public void Create_MeasuresTheAreaAndAnnouncesTheEvent()
    {
        var field = Field.Create(AFarm, "Talhao Sede", Crop.Soybean, OneHundredHectares());

        Assert.InRange(field.AreaHectares, 99.5m, 100.5m);
        Assert.Equal(Srid.Wgs84, field.Geometry.SRID);
        Assert.Single(field.DomainEvents);
        Assert.IsType<FieldCreatedEvent>(field.DomainEvents.Single());
    }

    [Fact]
    public void Create_BelowTheMinimumArea_IsRejected()
    {
        // Cerca de 12 m de lado: um clique acidental, nao um talhao.
        var tiny = Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.6000, -56.1000),
            GeoCoordinate.From(-15.6000, -56.0999),
            GeoCoordinate.From(-15.5999, -56.0999),
            GeoCoordinate.From(-15.5999, -56.1000),
        ]);

        var exception = Assert.Throws<DomainException>(
            () => Field.Create(AFarm, "Migalha", Crop.Soybean, tiny));

        Assert.Equal("field.area_below_minimum", exception.Error.Code);
        Assert.Equal(ErrorType.BusinessRule, exception.Error.Type);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutAName_IsRejected(string name)
    {
        var exception = Assert.Throws<DomainException>(
            () => Field.Create(AFarm, name, Crop.Soybean, OneHundredHectares()));

        Assert.Equal("field.name_required", exception.Error.Code);
    }

    [Fact]
    public void Create_WithACropOutsideTheCatalog_IsRejected()
    {
        // O inteiro convertido a forca no enum e o que chega quando o cliente inventa um valor.
        var exception = Assert.Throws<DomainException>(
            () => Field.Create(AFarm, "Talhao 1", (Crop)999, OneHundredHectares()));

        Assert.Equal("field.unknown_crop", exception.Error.Code);
    }

    [Fact]
    public void Name_IsStoredTrimmed()
    {
        var field = Field.Create(AFarm, "  Talhao Norte  ", Crop.Corn, OneHundredHectares());

        Assert.Equal("Talhao Norte", field.Name);
    }

    /// <summary>
    /// A area acompanha o contorno. Sao dois campos que nunca podem discordar, e a garantia disso e
    /// nao existir setter publico para a area.
    /// </summary>
    [Fact]
    public void Redraw_RewritesTheAreaAndAnnouncesTheChange()
    {
        var field = Field.Create(AFarm, "Talhao 1", Crop.Soybean, OneHundredHectares());
        field.ClearDomainEvents();

        var originalArea = field.AreaHectares;
        field.Redraw(OneHundredHectares(sides: 2));

        Assert.True(field.AreaHectares > originalArea);
        Assert.InRange(field.AreaHectares, 398m, 402m);

        var raised = Assert.IsType<FieldRedrawnEvent>(field.DomainEvents.Single());
        Assert.Equal(originalArea, raised.PreviousAreaHectares);
        Assert.Equal(field.AreaHectares, raised.CurrentAreaHectares);
        Assert.NotNull(field.UpdatedAt);
    }

    [Fact]
    public void Deactivate_KeepsTheFieldAndItsHistory()
    {
        var field = Field.Create(AFarm, "Talhao 1", Crop.Coffee, OneHundredHectares());

        Assert.True(field.Active);

        field.Deactivate();
        Assert.False(field.Active);
        Assert.NotNull(field.Geometry);

        field.Reactivate();
        Assert.True(field.Active);
    }

    /// <param name="sides">Multiplicador do lado; 2 quadruplica a area.</param>
    private static Boundary OneHundredHectares(int sides = 1)
    {
        var square = Wgs84GeodesyTests.OneKilometreSquare(-15.6, -56.1);

        if (sides == 1)
            return Boundary.FromCoordinates(square);

        var origin = square[0];
        var deltaLatitude = (square[2].Latitude - origin.Latitude) * sides;
        var deltaLongitude = (square[2].Longitude - origin.Longitude) * sides;

        return Boundary.FromCoordinates(
        [
            origin,
            GeoCoordinate.From(origin.Latitude, origin.Longitude + deltaLongitude),
            GeoCoordinate.From(origin.Latitude + deltaLatitude, origin.Longitude + deltaLongitude),
            GeoCoordinate.From(origin.Latitude + deltaLatitude, origin.Longitude),
        ]);
    }
}
