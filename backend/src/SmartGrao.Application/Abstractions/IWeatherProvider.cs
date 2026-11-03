using SmartGrao.Domain.Risk;

namespace SmartGrao.Application.Abstractions;

public sealed record WeatherReading(DateOnly Date, int Hour, double TemperatureC, double RelativeHumidity, WeatherSource Source);

/// <summary>Clima horario em hora local do ponto.</summary>
public interface IWeatherProvider
{
    /// <summary>Reanalise para um periodo ja encerrado. Chega com alguns dias de atraso.</summary>
    Task<IReadOnlyList<WeatherReading>> GetArchiveAsync(double latitude, double longitude, DateOnly from, DateOnly to, CancellationToken cancellationToken);

    /// <summary>Os ultimos <paramref name="pastDays"/> dias e a previsao dos proximos <paramref name="forecastDays"/>.</summary>
    Task<IReadOnlyList<WeatherReading>> GetRecentAsync(double latitude, double longitude, int pastDays, int forecastDays, CancellationToken cancellationToken);
}
