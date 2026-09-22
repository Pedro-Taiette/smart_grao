using SmartGrao.Application.Common;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Inspections;

// ── Pessoas ──────────────────────────────────────────────────────────────────

public sealed record CreatePersonViewModel(Guid FarmId, string Name, PersonRole Role);
public sealed record UpdatePersonViewModel(string Name, PersonRole Role);

public sealed record PersonViewModel(Guid Id, Guid FarmId, string Name, PersonRole Role, bool Active)
{
    public static PersonViewModel From(Person person)
    {
        ArgumentNullException.ThrowIfNull(person);
        return new(person.Id.Value, person.FarmId.Value, person.Name, person.Role, person.Active);
    }
}

// ── Vistorias ────────────────────────────────────────────────────────────────

public sealed record ScheduleInspectionViewModel(
    Guid CultivationId, Guid SamplingPlanId, Guid ProtocolId, Guid ResponsibleId, DateOnly ScheduledFor);

public sealed record RescheduleInspectionViewModel(Guid ResponsibleId, DateOnly ScheduledFor);

public sealed record CancelInspectionViewModel(string Reason);

public sealed record ObservationCountViewModel(Guid ProtocolItemId, decimal? Value, bool Detected);

public sealed record RecordObservationViewModel(
    Guid? SamplingPointId, DateTimeOffset RecordedAt, GeoCoordinateViewModel Location,
    double? AccuracyMeters, string? GrowthStage, string? Notes,
    IReadOnlyList<ObservationCountViewModel> Counts);

public sealed record TargetCountViewModel(
    Guid Id, Guid ProtocolItemId, Guid TargetId, string TargetCode, string TargetCommonName,
    CountUnit Unit, decimal? Value, bool Detected);

public sealed record ObservationViewModel(
    Guid Id, Guid? SamplingPointId, int? PointSequence, DateTimeOffset RecordedAt,
    GeoCoordinateViewModel Location, double? AccuracyMeters, bool HasPoorAccuracy,
    string? GrowthStage, string? Notes, bool IsOffPlan, IReadOnlyList<TargetCountViewModel> Counts);

/// <summary>Resumo para listas e para a agenda: não carrega as observações.</summary>
public sealed record InspectionSummaryViewModel(
    Guid Id, Guid FieldId, string FieldName, Guid CultivationId, Guid SamplingPlanId, Guid ProtocolId,
    Guid ResponsibleId, string ResponsibleName, Crop Crop, DateOnly ScheduledFor,
    InspectionStatus Status, DateTimeOffset? StartedAt, DateTimeOffset? CompletedAt,
    string? CancellationReason, int ObservationCount, int VisitedPointCount, int PointCount);

public sealed record InspectionViewModel(
    Guid Id, Guid FieldId, Guid CultivationId, Guid SamplingPlanId, Guid ProtocolId,
    string ProtocolName, int ProtocolVersion, Guid ResponsibleId, string ResponsibleName,
    Crop Crop, DateOnly ScheduledFor, InspectionStatus Status, DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt, string? CancellationReason, int PointCount,
    IReadOnlyList<ObservationViewModel> Observations);
