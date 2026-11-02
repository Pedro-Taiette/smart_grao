using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Features.Cultivations;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Application.Features.Risk;

public static class RiskClock
{
    /// <summary>Data civil de Brasilia, que nao tem mais horario de verao.</summary>
    public static DateOnly Today => DateOnly.FromDateTime(DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).DateTime);
}

internal static class RiskProjection
{
    public const int ForecastDays = 3;

    public static async Task<CercosporaRiskViewModel> BuildAsync(ISmartGraoDbContext db, Cultivation cultivation, CancellationToken cancellationToken)
    {
        var today = RiskClock.Today;
        var last = cultivation.EndedOn is { } ended && ended < today ? ended : today.AddDays(ForecastDays);
        var hours = await LoadHoursAsync(db, cultivation.FieldId, cultivation.PlantedOn, last, cancellationToken);
        var days = CercosporaRiskIndex.Compute(hours, cultivation.PlantedOn, last, cultivation.RiskFactors)
            .Select(day => ToViewModel(day, today)).ToList();
        var current = days.LastOrDefault(day => day.Date <= today && day.Level is not null);
        var stage = CercosporaRiskIndex.StageAt(cultivation.Stages, today);
        var suggests = cultivation.EndedOn is null && current is not null && current.Date == today
            && CercosporaRiskIndex.SuggestsInspection(ToDomain(current), stage);
        return new CercosporaRiskViewModel(
            cultivation.Id.Value, cultivation.FieldId.Value, today, current, ToViewModel(stage), suggests,
            CercosporaRiskFactorsViewModel.From(cultivation.RiskFactors),
            hours.Count == 0 ? null : hours.Max(x => x.RetrievedAt), CercosporaRiskIndex.WindowDays, days);
    }

    public static async Task<IReadOnlyList<WeatherHour>> LoadHoursAsync(
        ISmartGraoDbContext db, FieldId fieldId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var start = from.AddDays(-CercosporaRiskIndex.WindowDays);
        return await db.WeatherHours.AsNoTracking()
            .Where(x => x.FieldId == fieldId && x.Date >= start && x.Date <= to)
            .ToListAsync(cancellationToken);
    }

    public static DailyRiskViewModel ToViewModel(DailyRisk day, DateOnly today) => new(
        day.Date, day.Level, day.ClimateLevel, day.HumidNights, day.FavorableDays, day.DaysWithData, day.Date > today);

    public static StageStatusViewModel ToViewModel(StageStatus stage) =>
        new(stage.Window, stage.Stage, stage.ObservedOn, stage.IsStale);

    private static DailyRisk ToDomain(DailyRiskViewModel day) =>
        new(day.Date, day.HumidNights, day.FavorableDays, day.DaysWithData, day.ClimateLevel, day.Level);
}

public sealed class GetCercosporaRiskQueryHandler(ISmartGraoDbContext db)
{
    public async Task<CercosporaRiskViewModel> HandleAsync(Guid cultivationId, CancellationToken cancellationToken = default)
    {
        var cultivation = await LoadCornCultivationAsync(db, cultivationId, cancellationToken);
        return await RiskProjection.BuildAsync(db, cultivation, cancellationToken);
    }

    internal static async Task<Cultivation> LoadCornCultivationAsync(ISmartGraoDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var cultivationId = new CultivationId(id);
        var cultivation = await db.Cultivations.AsNoTracking().Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        if (cultivation.Crop != Crop.Corn) throw new DomainException(SmartGraoErrors.Risk.NotCorn);
        return cultivation;
    }
}

public sealed class RefreshWeatherCommandHandler(ISmartGraoDbContext db, IWeatherProvider provider)
{
    /// <summary>A reanalise chega com alguns dias de atraso; o periodo mais recente vem da previsao.</summary>
    public const int ArchiveLagDays = 7;

    public async Task<CercosporaRiskViewModel> HandleAsync(Guid cultivationId, CancellationToken cancellationToken = default)
    {
        var cultivation = await GetCercosporaRiskQueryHandler.LoadCornCultivationAsync(db, cultivationId, cancellationToken);
        var field = await db.Fields.AsNoTracking().FirstOrDefaultAsync(x => x.Id == cultivation.FieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);
        await RefreshAsync(cultivation, field, cancellationToken);
        return await RiskProjection.BuildAsync(db, cultivation, cancellationToken);
    }

    private async Task RefreshAsync(Cultivation cultivation, Field field, CancellationToken cancellationToken)
    {
        var center = field.Boundary.Center;
        var today = RiskClock.Today;
        var start = cultivation.PlantedOn.AddDays(-CercosporaRiskIndex.WindowDays);
        var end = cultivation.EndedOn is { } ended && ended < today ? ended : today;
        var archiveEnd = end < today.AddDays(-ArchiveLagDays) ? end : today.AddDays(-ArchiveLagDays);
        var readings = new List<WeatherReading>();

        if (start <= archiveEnd && !await HasArchiveAsync(cultivation.FieldId, start, archiveEnd, cancellationToken))
            readings.AddRange(await FetchAsync(() => provider.GetArchiveAsync(center.Latitude, center.Longitude, start, archiveEnd, cancellationToken)));

        if (end > archiveEnd)
        {
            var pastDays = Math.Min(today.DayNumber - archiveEnd.DayNumber, 92);
            var recent = await FetchAsync(() => provider.GetRecentAsync(
                center.Latitude, center.Longitude, pastDays, cultivation.EndedOn is null ? RiskProjection.ForecastDays : 0, cancellationToken));
            readings.AddRange(recent.Where(x => x.Date > archiveEnd && x.Date >= start));
        }

        if (readings.Count == 0) return;
        var from = readings.Min(x => x.Date);
        var to = readings.Max(x => x.Date);
        await db.WeatherHours.Where(x => x.FieldId == cultivation.FieldId && x.Date >= from && x.Date <= to)
            .ExecuteDeleteAsync(cancellationToken);
        var retrievedAt = DateTimeOffset.UtcNow;
        db.WeatherHours.AddRange(readings
            .DistinctBy(x => (x.Date, x.Hour))
            .Select(x => WeatherHour.Create(cultivation.FieldId, x.Date, x.Hour, x.TemperatureC, x.RelativeHumidity, x.Source, retrievedAt)));
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> HasArchiveAsync(FieldId fieldId, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var expected = (to.DayNumber - from.DayNumber + 1) * 24;
        var stored = await db.WeatherHours.CountAsync(x => x.FieldId == fieldId && x.Source == WeatherSource.Archive
            && x.Date >= from && x.Date <= to, cancellationToken);
        return stored >= expected;
    }

    private static async Task<IReadOnlyList<WeatherReading>> FetchAsync(Func<Task<IReadOnlyList<WeatherReading>>> fetch)
    {
        try
        {
            return await fetch();
        }
        catch (HttpRequestException)
        {
            throw new DomainException(SmartGraoErrors.Risk.WeatherUnavailable);
        }
    }
}

public sealed class GetCercosporaAlertsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<CercosporaAlertViewModel>> HandleAsync(Guid farmId, CancellationToken cancellationToken = default)
    {
        var farm = new Domain.Farms.FarmId(farmId);
        var fields = await db.Fields.AsNoTracking().Where(x => x.FarmId == farm)
            .Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var fieldIds = fields.Select(x => x.Id).ToList();
        var cultivations = await db.Cultivations.AsNoTracking().Include(x => x.Stages)
            .Where(x => fieldIds.Contains(x.FieldId) && x.Crop == Crop.Corn && x.EndedOn == null)
            .ToListAsync(cancellationToken);

        var alerts = new List<CercosporaAlertViewModel>();
        foreach (var cultivation in cultivations)
        {
            var risk = await RiskProjection.BuildAsync(db, cultivation, cancellationToken);
            alerts.Add(new CercosporaAlertViewModel(
                cultivation.Id.Value, cultivation.FieldId.Value, fields.First(x => x.Id == cultivation.FieldId).Name,
                cultivation.Cultivar, risk.Current, risk.Stage, risk.SuggestsInspection, risk.RiskFactors));
        }
        return alerts.OrderByDescending(x => x.Current?.Level).ThenBy(x => x.FieldName).ToList();
    }
}
