using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Risk;
using Xunit;

namespace SmartGrao.Domain.Tests.Risk;

public sealed class CercosporaRiskIndexTests
{
    private static readonly FieldId Field = FieldId.New();
    private static readonly DateOnly Today = new(2026, 1, 20);
    private static readonly DateTimeOffset Retrieved = new(2026, 1, 20, 0, 0, 0, TimeSpan.Zero);

    private static IEnumerable<WeatherHour> Days(DateOnly from, int count, Func<int, (double Temperature, double Humidity)> hour)
    {
        for (var day = 0; day < count; day++)
            for (var h = 0; h < 24; h++)
            {
                var (temperature, humidity) = hour(h);
                yield return WeatherHour.Create(Field, from.AddDays(day), h, temperature, humidity, WeatherSource.Archive, Retrieved);
            }
    }

    private static (double, double) WarmHumid(int hour) => hour is >= 20 or < 6 ? (23, 95) : (28, 70);
    private static (double, double) CoolDry(int hour) => (18, 60);

    private static DailyRisk Last(IEnumerable<WeatherHour> hours, CercosporaRiskFactors? factors = null) =>
        CercosporaRiskIndex.Compute(hours, Today, Today, factors ?? CercosporaRiskFactors.Unknown).Single();

    [Fact]
    public void FourteenWarmHumidDaysAreHighRisk()
    {
        var risk = Last(Days(Today.AddDays(-14), 15, WarmHumid));

        Assert.Equal(14, risk.HumidNights);
        Assert.Equal(14, risk.FavorableDays);
        Assert.Equal(RiskLevel.High, risk.ClimateLevel);
        Assert.Equal(RiskLevel.High, risk.Level);
    }

    [Fact]
    public void CoolDryWeatherIsLowRisk()
    {
        var risk = Last(Days(Today.AddDays(-14), 15, CoolDry));

        Assert.Equal(0, risk.HumidNights);
        Assert.Equal(RiskLevel.Low, risk.Level);
    }

    [Fact]
    public void FiveFavorableDaysOutOfFourteenAreModerate()
    {
        var hours = Days(Today.AddDays(-14), 10, CoolDry).Concat(Days(Today.AddDays(-4), 5, WarmHumid));

        var risk = Last(hours);

        Assert.Equal(RiskLevel.Moderate, risk.ClimateLevel);
    }

    [Fact]
    public void MissingWeatherYieldsNoLevel()
    {
        var risk = Last(Days(Today.AddDays(-5), 6, WarmHumid));

        Assert.True(risk.DaysWithData < CercosporaRiskIndex.MinimumDaysWithData);
        Assert.Null(risk.Level);
    }

    [Fact]
    public void AggravatingFactorsRaiseModerateToHigh()
    {
        var hours = Days(Today.AddDays(-14), 10, CoolDry).Concat(Days(Today.AddDays(-4), 5, WarmHumid)).ToList();
        var factors = CercosporaRiskFactors.Create(HybridReaction.Susceptible, CropSequence.CornAfterCorn, TillageSystem.Unknown);

        Assert.Equal(RiskLevel.High, Last(hours, factors).Level);
    }

    [Fact]
    public void MitigatingFactorsLowerHighToModerate()
    {
        var factors = CercosporaRiskFactors.Create(HybridReaction.Resistant, CropSequence.Rotation, TillageSystem.Unknown);

        Assert.Equal(RiskLevel.Moderate, Last(Days(Today.AddDays(-14), 15, WarmHumid), factors).Level);
    }

    [Fact]
    public void FactorsNeverRaiseLowClimate()
    {
        var factors = CercosporaRiskFactors.Create(HybridReaction.Susceptible, CropSequence.CornAfterCorn, TillageSystem.NoTill);

        Assert.Equal(RiskLevel.Low, Last(Days(Today.AddDays(-14), 15, CoolDry), factors).Level);
    }

    [Theory]
    [InlineData("V2", StageWindow.Before)]
    [InlineData("V4", StageWindow.Within)]
    [InlineData("VT", StageWindow.Within)]
    [InlineData("R2", StageWindow.Within)]
    [InlineData("R3", StageWindow.After)]
    public void StageWindowRunsFromV4ToR2(string stage, StageWindow expected)
    {
        var cultivation = Cultivation.Create(Field, SeasonId.New(), Crop.Corn, "A", Today.AddDays(-60));
        cultivation.RecordStage(Today.AddDays(-3), stage, null);

        var status = CercosporaRiskIndex.StageAt(cultivation.Stages, Today);

        Assert.Equal(expected, status.Window);
        Assert.False(status.IsStale);
    }

    [Fact]
    public void OldStageRecordIsStale()
    {
        var cultivation = Cultivation.Create(Field, SeasonId.New(), Crop.Corn, "A", Today.AddDays(-60));
        cultivation.RecordStage(Today.AddDays(-20), "V3", null);

        var status = CercosporaRiskIndex.StageAt(cultivation.Stages, Today);

        Assert.True(status.IsStale);
        Assert.True(CercosporaRiskIndex.SuggestsInspection(new DailyRisk(Today, 14, 14, 14, RiskLevel.High, RiskLevel.High), status));
    }

    [Fact]
    public void HighRiskAfterR2DoesNotSuggestInspection()
    {
        var cultivation = Cultivation.Create(Field, SeasonId.New(), Crop.Corn, "A", Today.AddDays(-100));
        cultivation.RecordStage(Today.AddDays(-2), "R4", null);
        var risk = new DailyRisk(Today, 14, 14, 14, RiskLevel.High, RiskLevel.High);

        Assert.False(CercosporaRiskIndex.SuggestsInspection(risk, CercosporaRiskIndex.StageAt(cultivation.Stages, Today)));
    }

    [Fact]
    public void ImplausibleWeatherIsRejected()
    {
        Assert.Throws<SmartGrao.Domain.Abstractions.DomainException>(() =>
            WeatherHour.Create(Field, Today, 24, 20, 50, WeatherSource.Archive, Retrieved));
        Assert.Throws<SmartGrao.Domain.Abstractions.DomainException>(() =>
            WeatherHour.Create(Field, Today, 10, 20, 120, WeatherSource.Archive, Retrieved));
    }
}
