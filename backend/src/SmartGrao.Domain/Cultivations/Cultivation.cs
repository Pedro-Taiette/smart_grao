using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Cultivations;

public readonly record struct CultivationId(Guid Value) : IStronglyTypedId
{
    public static CultivationId New() => new(Guid.CreateVersion7());
}

public readonly record struct GrowthStageRecordId(Guid Value) : IStronglyTypedId
{
    public static GrowthStageRecordId New() => new(Guid.CreateVersion7());
}

public sealed class GrowthStageRecord : Entity<GrowthStageRecordId>
{
    private GrowthStageRecord() { }
    public CultivationId CultivationId { get; private set; }
    public DateOnly ObservedOn { get; private set; }
    public string Stage { get; private set; } = string.Empty;
    public string? Notes { get; private set; }

    internal static GrowthStageRecord Create(CultivationId cultivationId, DateOnly observedOn, string stage, string? notes)
    {
        if (string.IsNullOrWhiteSpace(stage) || stage.Trim().Length > 32 || notes?.Length > 1000)
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidStage);
        return new GrowthStageRecord
        {
            Id = GrowthStageRecordId.New(), CultivationId = cultivationId, ObservedOn = observedOn,
            Stage = stage.Trim(), Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
        };
    }
}

/// <summary>One crop cycle. Its field, season, crop and planting date are immutable historical context.</summary>
public sealed class Cultivation : AggregateRoot<CultivationId>
{
    private readonly List<GrowthStageRecord> _stages = [];
    private Cultivation() { }
    public FieldId FieldId { get; private set; }
    public SeasonId SeasonId { get; private set; }
    public Crop Crop { get; private set; }
    public string Cultivar { get; private set; } = string.Empty;
    public DateOnly PlantedOn { get; private set; }
    public DateOnly? EndedOn { get; private set; }
    public IReadOnlyList<GrowthStageRecord> Stages => _stages.AsReadOnly();

    public static Cultivation Create(FieldId fieldId, SeasonId seasonId, Crop crop, string cultivar, DateOnly plantedOn)
    {
        if (!Enum.IsDefined(crop) || crop == Crop.Undefined)
            throw new DomainException(SmartGraoErrors.Field.UnknownCrop);
        if (string.IsNullOrWhiteSpace(cultivar) || cultivar.Trim().Length > 120)
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidCultivar);
        if (plantedOn == default)
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidDate);
        return new Cultivation
        {
            Id = CultivationId.New(), FieldId = fieldId, SeasonId = seasonId,
            Crop = crop, Cultivar = cultivar.Trim(), PlantedOn = plantedOn,
        };
    }

    public void Close(DateOnly endedOn)
    {
        if (EndedOn == endedOn) return;
        if (EndedOn.HasValue) throw new DomainException(SmartGraoErrors.Cultivation.Closed);
        if (endedOn < PlantedOn || _stages.Any(stage => stage.ObservedOn > endedOn))
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidDate);
        EndedOn = endedOn;
        MarkAsUpdated();
    }

    public void RecordStage(DateOnly observedOn, string stage, string? notes)
    {
        if (observedOn < PlantedOn || (EndedOn.HasValue && observedOn > EndedOn.Value))
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidDate);
        if (_stages.Any(record => record.ObservedOn == observedOn))
            throw new DomainException(SmartGraoErrors.Cultivation.DuplicateStageDate);
        // O codigo e resolvido pela escala da cultura antes de virar registro: "v6" e "V6" sao o
        // mesmo estadio, e gravar os dois separaria em duas coisas o que e uma so.
        if (!GrowthStageCatalog.TryResolve(Crop, stage, out var code))
            throw new DomainException(SmartGraoErrors.Cultivation.StageNotInScale);
        _stages.Add(GrowthStageRecord.Create(Id, observedOn, code, notes));
        MarkAsUpdated();
    }
}
