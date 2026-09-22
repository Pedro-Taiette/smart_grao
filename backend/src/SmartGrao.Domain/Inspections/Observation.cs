using NetTopologySuite.Geometries;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Domain.Inspections;

public readonly record struct ObservationId(Guid Value) : IStronglyTypedId
{
    public static ObservationId New() => new(Guid.CreateVersion7());
}

public readonly record struct TargetCountId(Guid Value) : IStronglyTypedId
{
    public static TargetCountId New() => new(Guid.CreateVersion7());
}

/// <summary>O que se anotou de um alvo numa parada. Entrada da coleta, antes de virar registro.</summary>
public sealed record ObservationCount(ProtocolItem Item, decimal? Value, bool Detected);

/// <summary>
/// A contagem de um alvo numa observacao, na unidade que o protocolo declarou para ele.
/// <para>
/// Existir uma linha por alvo <b>avaliado</b> — inclusive quando nada foi encontrado — e o que
/// sustenta a regra do roadmap: um resultado negativo precisa dizer quais alvos foram avaliados, e
/// nunca pode ser lido como "a planta esta saudavel". Alvo que ninguem olhou simplesmente nao tem
/// linha, e essa ausencia e visivel.
/// </para>
/// </summary>
public sealed class TargetCount : Entity<TargetCountId>
{
    private TargetCount() { }

    public ObservationId ObservationId { get; private set; }

    public ProtocolItemId ProtocolItemId { get; private set; }

    /// <summary>
    /// Alvo e unidade copiados do item do protocolo. A versao publicada e imutavel, entao nao ha
    /// como divergirem — e a observacao fica legivel, e exportavel na fase 7, sem carregar o
    /// protocolo junto.
    /// </summary>
    public MonitoringTargetId TargetId { get; private set; }

    public CountUnit Unit { get; private set; }

    /// <summary>Nulo no registro de presenca, que nao conta nada.</summary>
    public decimal? Value { get; private set; }

    /// <summary>
    /// Se o alvo foi encontrado. Nas unidades de contagem vem do proprio numero; no registro de
    /// presenca e a unica informacao que existe. Guardado tambem para as contagens porque "quais
    /// alvos apareceram nesta visita" e a pergunta das fases 5 e 6, e ela nao deveria depender de
    /// reinterpretar o valor em cada consulta.
    /// </summary>
    public bool Detected { get; private set; }

    internal static TargetCount Create(ObservationId observationId, ObservationCount count)
    {
        ArgumentNullException.ThrowIfNull(count);
        var item = count.Item ?? throw new DomainException(SmartGraoErrors.Inspection.UnknownCountTarget);

        var detected = count.Detected;
        var value = count.Value;

        if (item.Unit == CountUnit.Presence)
        {
            if (value is not null)
                throw new DomainException(SmartGraoErrors.Inspection.PresenceTakesNoValue);
        }
        else
        {
            if (value is not { } measured)
                throw new DomainException(SmartGraoErrors.Inspection.CountValueRequired);

            var (_, maximum) = item.Unit.ReferenceRange();
            if (measured < 0m || measured > maximum)
                throw new DomainException(SmartGraoErrors.Inspection.CountValueOutOfRange);

            // O numero manda: "3 lagartas, nao detectado" e uma contradicao que nao deve chegar ao banco.
            detected = measured > 0m;
        }

        return new TargetCount
        {
            Id = TargetCountId.New(),
            ObservationId = observationId,
            ProtocolItemId = item.Id,
            TargetId = item.TargetId,
            Unit = item.Unit,
            Value = value,
            Detected = detected,
        };
    }
}

/// <summary>
/// Uma parada da vistoria: onde se esteve, quando, em que estadio a lavoura estava e o que se
/// contou de cada alvo.
/// <para>
/// O GPS gravado e o <b>efetivo</b> — onde a pessoa realmente parou —, e nao a coordenada que o
/// plano mandava. Os dois raramente coincidem no mato, e e a diferenca entre eles que diz se a
/// caminhada seguiu a malha. Por isso a precisao vem junto: sem ela, nao se distingue um desvio
/// real de um erro do aparelho.
/// </para>
/// </summary>
public sealed class Observation : Entity<ObservationId>
{
    public const int MaximumNotesLength = 1000;

    /// <summary>
    /// Acima disso a leitura nao localiza uma parada dentro de um talhao — e um circulo maior que
    /// muitos talhoes inteiros. Nao bloqueia a coleta: a observacao entra com a precisao registrada,
    /// e quem for usar o dado decide. Bloquear faria a pessoa em campo perder o registro por causa
    /// do aparelho.
    /// </summary>
    public const double PoorAccuracyMeters = 30d;

    private readonly List<TargetCount> _counts = [];

    private Observation() { }

    public InspectionId InspectionId { get; private set; }

    /// <summary>
    /// Nulo quando a parada nao estava na malha. E o caso que o roadmap pede explicitamente: quem
    /// ve uma reboleira a caminho do proximo ponto precisa registra-la onde ela esta, e nao no ponto
    /// planejado mais proximo.
    /// </summary>
    public SamplingPointId? SamplingPointId { get; private set; }

    public DateTimeOffset RecordedAt { get; private set; }

    /// <summary>Onde se parou de fato, como o PostGIS guarda: <c>geography(Point,4326)</c>.</summary>
    public Point Location { get; private set; } = null!;

    /// <summary>Precisao informada pelo aparelho, em metros. Nulo quando o aparelho nao informa.</summary>
    public double? AccuracyMeters { get; private set; }

    /// <summary>Estádio da cultura na parada, validado pela escala da cultura do cultivo.</summary>
    public string? GrowthStage { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyList<TargetCount> Counts => _counts.AsReadOnly();

    public GeoCoordinate Coordinate => GeoCoordinate.FromCoordinate(Location.Coordinate);

    /// <summary>Ocorrência fora dos pontos planejados.</summary>
    public bool IsOffPlan => SamplingPointId is null;

    public bool HasPoorAccuracy => AccuracyMeters > PoorAccuracyMeters;

    /// <summary>Alvos em que algo foi encontrado — o resto da lista foi avaliado e deu nada.</summary>
    public IEnumerable<TargetCount> Detections => _counts.Where(count => count.Detected);

    internal static Observation Create(
        InspectionId inspectionId, SamplingPointId? pointId, DateTimeOffset recordedAt,
        GeoCoordinate location, double? accuracyMeters, string? growthStage, string? notes,
        IReadOnlyList<ObservationCount> counts)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(counts);

        if (accuracyMeters is { } accuracy && (double.IsNaN(accuracy) || double.IsInfinity(accuracy) || accuracy < 0d))
            throw new DomainException(SmartGraoErrors.Inspection.InvalidAccuracy);

        if (notes?.Trim().Length > MaximumNotesLength)
            throw new DomainException(SmartGraoErrors.Inspection.InvalidNotes);

        var observation = new Observation
        {
            Id = ObservationId.New(),
            InspectionId = inspectionId,
            SamplingPointId = pointId,
            RecordedAt = recordedAt,
            Location = location.ToPoint(),
            AccuracyMeters = accuracyMeters,
            GrowthStage = string.IsNullOrWhiteSpace(growthStage) ? null : growthStage.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };

        foreach (var count in counts)
        {
            if (observation._counts.Any(existing => existing.ProtocolItemId == count.Item.Id))
                throw new DomainException(SmartGraoErrors.Inspection.DuplicateCount);

            observation._counts.Add(TargetCount.Create(observation.Id, count));
        }

        return observation;
    }
}
