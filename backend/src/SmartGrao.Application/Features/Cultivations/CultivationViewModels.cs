using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed record CreateSeasonViewModel(Guid FarmId, string Name);
public sealed record SeasonViewModel(Guid Id, Guid FarmId, string Name)
{
    public static SeasonViewModel From(Season season) => new(season.Id.Value, season.FarmId.Value, season.Name);
}

public sealed record CreateCultivationViewModel(Guid FieldId, Guid SeasonId, Crop Crop, string Cultivar, DateOnly PlantedOn);
public sealed record CloseCultivationViewModel(DateOnly EndedOn);
public sealed record RecordGrowthStageViewModel(DateOnly ObservedOn, string Stage, string? Notes);

/// <summary>Um estádio da escala da cultura. Lista vazia significa cultura sem escala transcrita.</summary>
public sealed record GrowthStageOptionViewModel(string Code, string Description);

public sealed record GrowthStageViewModel(Guid Id, DateOnly ObservedOn, string Stage, string? Notes);
public sealed record CultivationViewModel(
    Guid Id, Guid FieldId, Guid SeasonId, Crop Crop, string Cultivar, DateOnly PlantedOn,
    DateOnly? EndedOn, IReadOnlyList<GrowthStageViewModel> Stages)
{
    public static CultivationViewModel From(Cultivation cultivation) => new(
        cultivation.Id.Value, cultivation.FieldId.Value, cultivation.SeasonId.Value,
        cultivation.Crop, cultivation.Cultivar, cultivation.PlantedOn, cultivation.EndedOn,
        cultivation.Stages.OrderByDescending(x => x.ObservedOn)
            .Select(x => new GrowthStageViewModel(x.Id.Value, x.ObservedOn, x.Stage, x.Notes)).ToList());
}
