using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Domain.Photos;

public readonly record struct PhotoId(Guid Value) : IStronglyTypedId;

/// <summary>Quem tirou a foto. O modelo de visao nao distingue; o aferimento sim.</summary>
public enum PhotoSource
{
    Phone = 0,
    Station,
    Drone,
}

/// <summary>
/// A foto de uma folha. O id vem do cliente: reenviar a mesma foto depois de uma falha de rede
/// encontra a que ja chegou, em vez de criar outra.
/// </summary>
public sealed class Photo : AggregateRoot<PhotoId>
{
    public const long MaximumBytes = 10 * 1024 * 1024;
    public static readonly IReadOnlySet<string> AcceptedContentTypes = new HashSet<string> { "image/jpeg", "image/png", "image/webp" };

    private Photo() { }

    public FieldId FieldId { get; private set; }

    public CultivationId CultivationId { get; private set; }

    public InspectionId? InspectionId { get; private set; }

    public ObservationId? ObservationId { get; private set; }

    public MonitoringTargetId? TargetId { get; private set; }

    public PlantOrgan Organ { get; private set; }

    public string? GrowthStage { get; private set; }

    public Point? Location { get; private set; }

    public double? AccuracyMeters { get; private set; }

    public PhotoSource Source { get; private set; }

    /// <summary>Gerada por simulador (estacao sentinela sem hardware). A tela sempre diz isso.</summary>
    public bool IsSimulated { get; private set; }

    public DateTimeOffset TakenAt { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public string StorageKey { get; private set; } = string.Empty;

    public static Photo FromObservation(
        PhotoId id, Inspection inspection, Observation observation, MonitoringTargetId? targetId, PlantOrgan organ,
        DateTimeOffset takenAt, string contentType, long sizeBytes)
    {
        ArgumentNullException.ThrowIfNull(inspection);
        ArgumentNullException.ThrowIfNull(observation);
        if (observation.InspectionId != inspection.Id)
            throw new DomainException(SmartGraoErrors.Photo.ObservationFromAnotherInspection);
        var photo = Create(id, inspection.FieldId, inspection.CultivationId, organ, takenAt, contentType, sizeBytes, PhotoSource.Phone, false);
        photo.InspectionId = inspection.Id;
        photo.ObservationId = observation.Id;
        photo.TargetId = targetId;
        photo.GrowthStage = observation.GrowthStage;
        photo.Location = observation.Location;
        photo.AccuracyMeters = observation.AccuracyMeters;
        return photo;
    }

    public static Photo FromDevice(
        PhotoId id, Cultivation cultivation, PhotoSource source, bool isSimulated, PlantOrgan organ,
        DateTimeOffset takenAt, GeoCoordinate? location, string contentType, long sizeBytes)
    {
        ArgumentNullException.ThrowIfNull(cultivation);
        if (source == PhotoSource.Phone)
            throw new DomainException(SmartGraoErrors.Photo.PhoneNeedsObservation);
        var photo = Create(id, cultivation.FieldId, cultivation.Id, organ, takenAt, contentType, sizeBytes, source, isSimulated);
        photo.Location = location?.ToPoint();
        return photo;
    }

    private static Photo Create(
        PhotoId id, FieldId fieldId, CultivationId cultivationId, PlantOrgan organ, DateTimeOffset takenAt,
        string contentType, long sizeBytes, PhotoSource source, bool isSimulated)
    {
        if (id.Value == Guid.Empty)
            throw new DomainException(SmartGraoErrors.Photo.InvalidId);
        if (!AcceptedContentTypes.Contains(contentType))
            throw new DomainException(SmartGraoErrors.Photo.UnsupportedType);
        if (sizeBytes <= 0 || sizeBytes > MaximumBytes)
            throw new DomainException(SmartGraoErrors.Photo.TooLarge);
        if (!Enum.IsDefined(organ) || organ == PlantOrgan.Undefined || !Enum.IsDefined(source))
            throw new DomainException(SmartGraoErrors.Photo.InvalidOrgan);
        return new Photo
        {
            Id = id, FieldId = fieldId, CultivationId = cultivationId, Organ = organ, TakenAt = takenAt.ToUniversalTime(),
            ContentType = contentType, SizeBytes = sizeBytes, Source = source, IsSimulated = isSimulated,
            StorageKey = $"{fieldId.Value:N}/{id.Value:N}{Extension(contentType)}",
        };
    }

    private static string Extension(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => ".jpg",
    };
}
