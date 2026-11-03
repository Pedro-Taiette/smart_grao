using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Photos;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;
using Xunit;

namespace SmartGrao.Domain.Tests.Photos;

public sealed class PhotoTests
{
    private static readonly DateOnly Planting = new(2026, 1, 10);
    private static readonly DateTimeOffset Morning = new(2026, 2, 1, 8, 0, 0, TimeSpan.FromHours(-3));

    private static (Inspection Inspection, Observation Observation, Cultivation Cultivation) Stop()
    {
        var cultivation = Cultivation.Create(FieldId.New(), SeasonId.New(), Crop.Corn, "AG 8700", Planting);
        var plan = SamplingPlan.ForMapping(cultivation.FieldId, Boundary.FromCoordinates(
        [
            GeoCoordinate.From(-15.6, -56.1), GeoCoordinate.From(-15.6, -56.09),
            GeoCoordinate.From(-15.59, -56.09), GeoCoordinate.From(-15.59, -56.1),
        ]), SamplingSpacing.Standard, cultivation.Id);
        var target = MonitoringTarget.Create("cercospora_zeina", "Cercosporiose", "Cercospora zeina", TargetKind.FoliarDisease, Crop.Corn);
        var protocol = Protocol.CreateDraft("cercosporiose_deteccao", Crop.Corn, "Cercosporiose");
        protocol.AddItem(target, PlantOrgan.LowerLeaves, CountUnit.AttackedPlantPercentage, 2, "Folhas de baixo.");
        protocol.Publish();
        var inspection = Inspection.Schedule(cultivation, plan, protocol, Person.Create(FarmId.New(), "Ana", PersonRole.Technician), Planting.AddDays(30));
        inspection.Start(Morning);
        var observation = inspection.RecordObservation(plan.Points[0], Morning, plan.Points[0].Coordinate, 5, "V8", null,
            [new ObservationCount(protocol.Items[0], 30m, true)]);
        return (inspection, observation, cultivation);
    }

    [Fact]
    public void PhotoCarriesTheStopContext()
    {
        var (inspection, observation, _) = Stop();
        var id = new PhotoId(Guid.NewGuid());

        var photo = Photo.FromObservation(id, inspection, observation, null, PlantOrgan.LowerLeaves, Morning, "image/jpeg", 2_000_000);

        Assert.Equal(id, photo.Id);
        Assert.Equal(inspection.FieldId, photo.FieldId);
        Assert.Equal(observation.Id, photo.ObservationId);
        Assert.Equal("V8", photo.GrowthStage);
        Assert.Equal(PhotoSource.Phone, photo.Source);
        Assert.False(photo.IsSimulated);
        Assert.EndsWith(".jpg", photo.StorageKey);
        Assert.Equal(TimeSpan.Zero, photo.TakenAt.Offset);
        Assert.Equal(TimeSpan.Zero, observation.RecordedAt.Offset);
    }

    [Theory]
    [InlineData("image/gif", 1_000)]
    [InlineData("image/jpeg", 0)]
    [InlineData("image/jpeg", Photo.MaximumBytes + 1)]
    public void InvalidFilesAreRejected(string contentType, long size)
    {
        var (inspection, observation, _) = Stop();

        Assert.Throws<DomainException>(() => Photo.FromObservation(
            new PhotoId(Guid.NewGuid()), inspection, observation, null, PlantOrgan.LowerLeaves, Morning, contentType, size));
    }

    [Fact]
    public void ClientMustGenerateTheId()
    {
        var (inspection, observation, _) = Stop();

        var error = Assert.Throws<DomainException>(() => Photo.FromObservation(
            new PhotoId(Guid.Empty), inspection, observation, null, PlantOrgan.LowerLeaves, Morning, "image/jpeg", 1_000));
        Assert.Equal(SmartGraoErrors.Photo.InvalidId.Code, error.Error.Code);
    }

    [Fact]
    public void StationPhotoIsMarkedSimulated()
    {
        var (_, _, cultivation) = Stop();

        var photo = Photo.FromDevice(new PhotoId(Guid.NewGuid()), cultivation, PhotoSource.Station, true,
            PlantOrgan.LowerLeaves, Morning, GeoCoordinate.From(-15.595, -56.095), "image/png", 50_000);

        Assert.True(photo.IsSimulated);
        Assert.Null(photo.ObservationId);
        Assert.NotNull(photo.Location);
    }

    [Fact]
    public void PhonePhotoNeedsAStop()
    {
        var (_, _, cultivation) = Stop();

        Assert.Throws<DomainException>(() => Photo.FromDevice(new PhotoId(Guid.NewGuid()), cultivation, PhotoSource.Phone, false,
            PlantOrgan.LowerLeaves, Morning, null, "image/png", 50_000));
    }
}
