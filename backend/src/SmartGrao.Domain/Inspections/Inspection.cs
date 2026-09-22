using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Domain.Inspections;

public readonly record struct InspectionId(Guid Value) : IStronglyTypedId
{
    public static InspectionId New() => new(Guid.CreateVersion7());
}

public enum InspectionStatus
{
    /// <summary>Agendada. Ninguem foi a campo ainda.</summary>
    Scheduled = 0,

    /// <summary>Em andamento. E o unico estado que aceita observacoes.</summary>
    InProgress,

    /// <summary>Concluída e imutável.</summary>
    Completed,

    /// <summary>Não aconteceu. Guarda o motivo, e não vira uma vistoria vazia no histórico.</summary>
    Cancelled,
}

/// <summary>
/// Uma visita ao talhão: quem foi, quando, seguindo qual malha e qual protocolo, e o que anotou em
/// cada parada.
/// <para>
/// A vistoria é o agregado, e não o plano. Um plano é caminhado muitas vezes — o MIP pede
/// amostragem semanal — e cada caminhada é uma vistoria própria, com as suas observações. É isso
/// que faz o critério da fase 3 se sustentar: executar, concluir e repetir, preservando cada visita.
/// </para>
/// <para>
/// Aponta para uma <b>versão</b> de protocolo, não para a família. A versão publicada é imutável,
/// então o que foi pedido nesta visita continua legível exatamente como estava no dia — mesmo
/// depois de a v2 mudar tudo.
/// </para>
/// </summary>
public sealed class Inspection : AggregateRoot<InspectionId>
{
    public const int MaximumReasonLength = 500;

    private readonly List<Observation> _observations = [];

    private Inspection() { }

    public FieldId FieldId { get; private set; }

    public CultivationId CultivationId { get; private set; }

    public SamplingPlanId SamplingPlanId { get; private set; }

    public ProtocolId ProtocolId { get; private set; }

    public PersonId ResponsibleId { get; private set; }

    /// <summary>
    /// A cultura do cultivo, copiada no agendamento. O cultivo nunca troca de cultura, então não há
    /// como divergir — e é ela que valida o estádio de cada observação contra a escala certa sem
    /// carregar o cultivo junto.
    /// </summary>
    public Crop Crop { get; private set; }

    public DateOnly ScheduledFor { get; private set; }

    public InspectionStatus Status { get; private set; }

    public DateTimeOffset? StartedAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<Observation> Observations => _observations.AsReadOnly();

    public bool IsFinished => Status is InspectionStatus.Completed or InspectionStatus.Cancelled;

    /// <summary>Paradas da malha já visitadas nesta vistoria.</summary>
    public int VisitedPointCount => _observations.Count(observation => !observation.IsOffPlan);

    /// <summary>Ocorrências registradas fora dos pontos planejados.</summary>
    public int OffPlanObservationCount => _observations.Count(observation => observation.IsOffPlan);

    public static Inspection Schedule(
        Cultivation cultivation, SamplingPlan plan, Protocol protocol, Person responsible,
        DateOnly scheduledFor)
    {
        ArgumentNullException.ThrowIfNull(cultivation);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(responsible);

        if (cultivation.EndedOn is not null)
            throw new DomainException(SmartGraoErrors.Cultivation.Closed);
        if (plan.FieldId != cultivation.FieldId || plan.CultivationId != cultivation.Id)
            throw new DomainException(SmartGraoErrors.Inspection.PlanFromAnotherCultivation);
        if (protocol.Status != ProtocolStatus.Published)
            throw new DomainException(SmartGraoErrors.Inspection.ProtocolNotPublished);
        if (protocol.Crop != cultivation.Crop)
            throw new DomainException(SmartGraoErrors.Inspection.ProtocolFromAnotherCrop);
        if (!responsible.Active)
            throw new DomainException(SmartGraoErrors.Inspection.PersonInactive);
        if (scheduledFor == default)
            throw new DomainException(SmartGraoErrors.Inspection.InvalidDate);
        if (scheduledFor < cultivation.PlantedOn)
            throw new DomainException(SmartGraoErrors.Inspection.InvalidDate);

        return new Inspection
        {
            Id = InspectionId.New(),
            FieldId = cultivation.FieldId,
            CultivationId = cultivation.Id,
            SamplingPlanId = plan.Id,
            ProtocolId = protocol.Id,
            ResponsibleId = responsible.Id,
            Crop = cultivation.Crop,
            ScheduledFor = scheduledFor,
            Status = InspectionStatus.Scheduled,
        };
    }

    /// <summary>Troca o responsável ou a data. Só enquanto ninguém foi a campo.</summary>
    public void Reschedule(Person responsible, DateOnly scheduledFor)
    {
        ArgumentNullException.ThrowIfNull(responsible);
        EnsureScheduled();

        if (!responsible.Active) throw new DomainException(SmartGraoErrors.Inspection.PersonInactive);
        if (scheduledFor == default) throw new DomainException(SmartGraoErrors.Inspection.InvalidDate);

        ResponsibleId = responsible.Id;
        ScheduledFor = scheduledFor;
        MarkAsUpdated();
    }

    public void Start(DateTimeOffset startedAt)
    {
        if (Status == InspectionStatus.InProgress) return;
        EnsureScheduled();

        Status = InspectionStatus.InProgress;
        StartedAt = startedAt;
        MarkAsUpdated();
    }

    /// <summary>
    /// Registra uma parada. O ponto é opcional — sem ele, é uma ocorrência fora da malha.
    /// <para>
    /// Cada alvo avaliado entra como uma contagem, mesmo quando não se achou nada. É o que impede a
    /// visita de ser lida depois como "estava tudo limpo" quando na verdade só metade dos alvos foi
    /// olhada.
    /// </para>
    /// </summary>
    public Observation RecordObservation(
        SamplingPoint? point, DateTimeOffset recordedAt, GeoCoordinate location,
        double? accuracyMeters, string? growthStage, string? notes,
        IReadOnlyList<ObservationCount> counts)
    {
        ArgumentNullException.ThrowIfNull(counts);

        if (Status != InspectionStatus.InProgress)
            throw new DomainException(SmartGraoErrors.Inspection.NotInProgress);

        if (point is not null)
        {
            if (point.SamplingPlanId != SamplingPlanId)
                throw new DomainException(SmartGraoErrors.Inspection.PointFromAnotherPlan);
            if (_observations.Any(existing => existing.SamplingPointId == point.Id))
                throw new DomainException(SmartGraoErrors.Inspection.DuplicatePointObservation);
        }

        foreach (var count in counts)
        {
            if (count.Item is null)
                throw new DomainException(SmartGraoErrors.Inspection.UnknownCountTarget);
            if (count.Item.ProtocolId != ProtocolId)
                throw new DomainException(SmartGraoErrors.Inspection.CountFromAnotherProtocol);
        }

        // O estadio segue a escala da cultura, como na fase 2 — "seis folhas" nao e estadio.
        string? resolvedStage = null;
        if (!string.IsNullOrWhiteSpace(growthStage))
        {
            if (!GrowthStageCatalog.TryResolve(Crop, growthStage, out var code))
                throw new DomainException(SmartGraoErrors.Cultivation.StageNotInScale);
            resolvedStage = code;
        }

        var observation = Observation.Create(
            Id, point?.Id, recordedAt, location, accuracyMeters, resolvedStage, notes, counts);

        _observations.Add(observation);
        MarkAsUpdated();

        return observation;
    }

    /// <summary>
    /// Conclui a visita. Exige ao menos uma observação: uma vistoria sem nada registrado não é uma
    /// vistoria concluída — é uma que não aconteceu, e para isso existe o cancelamento com motivo.
    /// </summary>
    public void Complete(DateTimeOffset completedAt)
    {
        if (Status == InspectionStatus.Completed) return;
        if (Status != InspectionStatus.InProgress)
            throw new DomainException(SmartGraoErrors.Inspection.NotInProgress);
        if (_observations.Count == 0)
            throw new DomainException(SmartGraoErrors.Inspection.NoObservations);
        if (StartedAt is { } startedAt && completedAt < startedAt)
            throw new DomainException(SmartGraoErrors.Inspection.InvalidTiming);

        Status = InspectionStatus.Completed;
        CompletedAt = completedAt;
        MarkAsUpdated();
    }

    public void Cancel(string reason)
    {
        if (Status == InspectionStatus.Cancelled) return;
        if (Status == InspectionStatus.Completed)
            throw new DomainException(SmartGraoErrors.Inspection.AlreadyFinished);
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaximumReasonLength)
            throw new DomainException(SmartGraoErrors.Inspection.CancellationNeedsReason);

        Status = InspectionStatus.Cancelled;
        CancellationReason = reason.Trim();
        MarkAsUpdated();
    }

    private void EnsureScheduled()
    {
        if (Status != InspectionStatus.Scheduled)
            throw new DomainException(SmartGraoErrors.Inspection.NotScheduled);
    }
}
