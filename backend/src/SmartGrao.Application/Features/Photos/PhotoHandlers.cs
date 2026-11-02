using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.Photos;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Photos;

public sealed record PhotoViewModel(
    Guid Id, Guid FieldId, Guid CultivationId, Guid? InspectionId, Guid? ObservationId, Guid? TargetId,
    PlantOrgan Organ, string? GrowthStage, GeoJsonPoint? Location, double? AccuracyMeters, PhotoSource Source,
    bool IsSimulated, DateTimeOffset TakenAt, string ContentType, long SizeBytes)
{
    public static PhotoViewModel From(Photo photo) => new(
        photo.Id.Value, photo.FieldId.Value, photo.CultivationId.Value, photo.InspectionId?.Value, photo.ObservationId?.Value,
        photo.TargetId?.Value, photo.Organ, photo.GrowthStage,
        photo.Location is null ? null : GeoJsonPoint.From(GeoCoordinate.FromCoordinate(photo.Location.Coordinate)),
        photo.AccuracyMeters, photo.Source, photo.IsSimulated, photo.TakenAt, photo.ContentType, photo.SizeBytes);
}

public sealed record UploadObservationPhoto(
    Guid Id, Guid InspectionId, Guid ObservationId, Guid? TargetId, PlantOrgan Organ, DateTimeOffset TakenAt,
    string ContentType, long SizeBytes, Stream Content);

public sealed record UploadDevicePhoto(
    Guid Id, Guid CultivationId, PhotoSource Source, bool IsSimulated, PlantOrgan Organ, DateTimeOffset TakenAt,
    double? Latitude, double? Longitude, string ContentType, long SizeBytes, Stream Content);

public sealed class UploadPhotoCommandHandler(ISmartGraoDbContext db, IPhotoStorage storage)
{
    public async Task<PhotoViewModel> HandleAsync(UploadObservationPhoto model, CancellationToken cancellationToken = default)
    {
        if (await ExistingAsync(model.Id, cancellationToken) is { } existing)
            return existing.ObservationId?.Value == model.ObservationId
                ? PhotoViewModel.From(existing)
                : throw new DomainException(SmartGraoErrors.Photo.IdTaken);

        var inspectionId = new InspectionId(model.InspectionId);
        var inspection = await db.Inspections.Include(x => x.Observations)
            .FirstOrDefaultAsync(x => x.Id == inspectionId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.NotFound);
        var observation = inspection.Observations.FirstOrDefault(x => x.Id.Value == model.ObservationId)
            ?? throw new DomainException(SmartGraoErrors.Photo.ObservationNotFound);
        var photo = Photo.FromObservation(new PhotoId(model.Id), inspection, observation,
            model.TargetId is { } target ? new MonitoringTargetId(target) : null,
            model.Organ, model.TakenAt, model.ContentType, model.SizeBytes);
        return await StoreAsync(photo, model.Content, cancellationToken);
    }

    public async Task<PhotoViewModel> HandleAsync(UploadDevicePhoto model, CancellationToken cancellationToken = default)
    {
        if (await ExistingAsync(model.Id, cancellationToken) is { } existing)
            return existing.CultivationId.Value == model.CultivationId
                ? PhotoViewModel.From(existing)
                : throw new DomainException(SmartGraoErrors.Photo.IdTaken);

        var cultivationId = new CultivationId(model.CultivationId);
        var cultivation = await db.Cultivations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        var location = model.Latitude is { } latitude && model.Longitude is { } longitude ? GeoCoordinate.From(latitude, longitude) : null;
        var photo = Photo.FromDevice(new PhotoId(model.Id), cultivation, model.Source, model.IsSimulated, model.Organ,
            model.TakenAt, location, model.ContentType, model.SizeBytes);
        return await StoreAsync(photo, model.Content, cancellationToken);
    }

    private Task<Photo?> ExistingAsync(Guid id, CancellationToken cancellationToken)
    {
        var photoId = new PhotoId(id);
        return db.Photos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == photoId, cancellationToken);
    }

    private async Task<PhotoViewModel> StoreAsync(Photo photo, Stream content, CancellationToken cancellationToken)
    {
        await storage.SaveAsync(photo.StorageKey, content, cancellationToken);
        db.Photos.Add(photo);
        await db.SaveChangesAsync(cancellationToken);
        return PhotoViewModel.From(photo);
    }
}

public sealed class GetPhotosQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<PhotoViewModel>> HandleAsync(Guid? inspectionId, Guid? cultivationId, CancellationToken cancellationToken = default)
    {
        var query = db.Photos.AsNoTracking();
        if (inspectionId is { } inspection)
        {
            var id = new InspectionId(inspection);
            query = query.Where(x => x.InspectionId == id);
        }
        if (cultivationId is { } cultivation)
        {
            var id = new CultivationId(cultivation);
            query = query.Where(x => x.CultivationId == id);
        }
        if (inspectionId is null && cultivationId is null)
            throw new DomainException(SmartGraoErrors.Common.ValidationFailed("Inform inspectionId or cultivationId."));
        var photos = await query.OrderBy(x => x.TakenAt).ToListAsync(cancellationToken);
        return photos.Select(PhotoViewModel.From).ToList();
    }
}

public sealed class GetPhotoContentQueryHandler(ISmartGraoDbContext db, IPhotoStorage storage)
{
    public async Task<(Stream Content, string ContentType)> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var photoId = new PhotoId(id);
        var photo = await db.Photos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == photoId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Photo.NotFound);
        var content = await storage.OpenAsync(photo.StorageKey, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Photo.NotFound);
        return (content, photo.ContentType);
    }
}
