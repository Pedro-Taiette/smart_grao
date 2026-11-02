using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Risk;

public enum RiskLevel
{
    Low = 0,
    Moderate,
    High,
}

public enum StageWindow
{
    Unknown = 0,
    Before,
    Within,
    After,
}

public sealed record DailyRisk(
    DateOnly Date, int HumidNights, int FavorableDays, int DaysWithData,
    RiskLevel? ClimateLevel, RiskLevel? Level);

public sealed record StageStatus(StageWindow Window, string? Stage, DateOnly? ObservedOn, bool IsStale);

/// <summary>
/// Indice de favorabilidade climatica, nao calibrado. Faixa de 22–30 °C, UR ≥ 90% nas horas noturnas
/// e a janela V4 em diante vem de Paul &amp; Munkvold (2005); a janela movel de 14 dias e o periodo
/// latente mais curto (Casela &amp; Ferreira, 2003). Os cortes entre niveis sao decisao de projeto.
/// Ver <c>docs/fase-5-risco-climatico.md</c>.
/// </summary>
public static class CercosporaRiskIndex
{
    public const int WindowDays = 14;
    public const int MinimumDaysWithData = 10;
    public const double MinimumTemperature = 22;
    public const double MaximumTemperature = 30;
    public const double HumidThreshold = 90;
    public const int NightStartHour = 20;
    public const int NightEndHour = 6;
    public const int HumidHoursPerNight = 6;
    public const int FavorableHoursPerDay = 6;
    public const int HighDays = 7;
    public const int ModerateDays = 4;
    public const int StaleStageDays = 14;
    public const string WindowStart = "V4";
    public const string WindowEnd = "R2";

    public static IReadOnlyList<DailyRisk> Compute(
        IEnumerable<WeatherHour> hours, DateOnly from, DateOnly to, CercosporaRiskFactors factors)
    {
        var byDay = hours.GroupBy(x => x.Date).ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Hour));
        var result = new List<DailyRisk>();
        for (var date = from; date <= to; date = date.AddDays(1))
        {
            int humid = 0, favorable = 0, withData = 0;
            for (var day = date.AddDays(1 - WindowDays); day <= date; day = day.AddDays(1))
            {
                var night = HumidNight(byDay, day);
                var warm = FavorableDay(byDay, day);
                if (night is null || warm is null) continue;
                withData++;
                if (night.Value) humid++;
                if (warm.Value) favorable++;
            }
            RiskLevel? climate = withData < MinimumDaysWithData ? null : ClimateLevel(humid, favorable);
            result.Add(new DailyRisk(date, humid, favorable, withData, climate, climate is { } c ? Adjust(c, factors) : null));
        }
        return result;
    }

    public static StageStatus StageAt(IEnumerable<GrowthStageRecord> stages, DateOnly date)
    {
        var latest = stages.Where(x => x.ObservedOn <= date).MaxBy(x => x.ObservedOn);
        if (latest is null) return new StageStatus(StageWindow.Unknown, null, null, false);
        var scale = GrowthStageCatalog.For(Crop.Corn).Select(x => x.Code).ToList();
        var index = scale.IndexOf(latest.Stage);
        var window = index < 0 ? StageWindow.Unknown
            : index < scale.IndexOf(WindowStart) ? StageWindow.Before
            : index > scale.IndexOf(WindowEnd) ? StageWindow.After
            : StageWindow.Within;
        var stale = window is not StageWindow.After && date.DayNumber - latest.ObservedOn.DayNumber > StaleStageDays;
        return new StageStatus(window, latest.Stage, latest.ObservedOn, stale);
    }

    /// <summary>Estadio desconhecido ou desatualizado nao bloqueia a sugestao: a tela pede a atualizacao.</summary>
    public static bool SuggestsInspection(DailyRisk risk, StageStatus stage) =>
        risk.Level == RiskLevel.High && stage.Window is StageWindow.Within or StageWindow.Unknown
        || risk.Level == RiskLevel.High && stage.Window is StageWindow.Before && stage.IsStale;

    private static RiskLevel ClimateLevel(int humidNights, int favorableDays) =>
        humidNights >= HighDays && favorableDays >= HighDays ? RiskLevel.High
        : humidNights >= ModerateDays && favorableDays >= ModerateDays ? RiskLevel.Moderate
        : RiskLevel.Low;

    private static RiskLevel Adjust(RiskLevel climate, CercosporaRiskFactors factors)
    {
        if (climate == RiskLevel.Low) return climate;
        var aggravating = (factors.HybridReaction is HybridReaction.Susceptible or HybridReaction.ModeratelySusceptible ? 1 : 0)
            + (factors.CropSequence == CropSequence.CornAfterCorn ? 1 : 0)
            + (factors.Tillage == TillageSystem.NoTill ? 1 : 0);
        var mitigating = (factors.HybridReaction is HybridReaction.Resistant or HybridReaction.ModeratelyResistant ? 1 : 0)
            + (factors.CropSequence == CropSequence.Rotation ? 1 : 0)
            + (factors.Tillage == TillageSystem.Conventional ? 1 : 0);
        if (aggravating >= 2 && climate < RiskLevel.High) return climate + 1;
        if (mitigating >= 2) return climate - 1;
        return climate;
    }

    private static bool? HumidNight(Dictionary<DateOnly, Dictionary<int, WeatherHour>> byDay, DateOnly day)
    {
        var evening = byDay.GetValueOrDefault(day.AddDays(-1));
        var morning = byDay.GetValueOrDefault(day);
        var night = Enumerable.Range(NightStartHour, 24 - NightStartHour).Select(h => evening?.GetValueOrDefault(h))
            .Concat(Enumerable.Range(0, NightEndHour).Select(h => morning?.GetValueOrDefault(h)))
            .OfType<WeatherHour>().ToList();
        if (night.Count < (24 - NightStartHour + NightEndHour) * 8 / 10) return null;
        return night.Count(x => x.RelativeHumidity >= HumidThreshold) >= HumidHoursPerNight;
    }

    private static bool? FavorableDay(Dictionary<DateOnly, Dictionary<int, WeatherHour>> byDay, DateOnly day)
    {
        if (!byDay.TryGetValue(day, out var hours) || hours.Count < 20) return null;
        return hours.Values.Count(x => x.TemperatureC is >= MinimumTemperature and <= MaximumTemperature) >= FavorableHoursPerDay;
    }
}
