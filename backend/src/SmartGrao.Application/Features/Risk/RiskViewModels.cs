using SmartGrao.Application.Features.Cultivations;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Application.Features.Risk;

public sealed record DailyRiskViewModel(
    DateOnly Date, RiskLevel? Level, RiskLevel? ClimateLevel, int HumidNights, int FavorableDays,
    int DaysWithData, bool IsForecast);

public sealed record StageStatusViewModel(StageWindow Window, string? Stage, DateOnly? ObservedOn, bool IsStale);

public sealed record CercosporaRiskViewModel(
    Guid CultivationId, Guid FieldId, DateOnly Today, DailyRiskViewModel? Current, StageStatusViewModel Stage,
    bool SuggestsInspection, CercosporaRiskFactorsViewModel RiskFactors, DateTimeOffset? WeatherRetrievedAt,
    int WindowDays, IReadOnlyList<DailyRiskViewModel> Days);

public sealed record CercosporaAlertViewModel(
    Guid CultivationId, Guid FieldId, string FieldName, string Cultivar, DailyRiskViewModel? Current,
    StageStatusViewModel Stage, bool SuggestsInspection, CercosporaRiskFactorsViewModel RiskFactors);
