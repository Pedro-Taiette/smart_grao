using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Infrastructure.Weather;

/// <summary>Open-Meteo: previsao (api.open-meteo.com) e reanalise ERA5 (archive-api.open-meteo.com), sem chave.</summary>
internal sealed class OpenMeteoWeatherProvider(HttpClient http) : IWeatherProvider
{
    private const string Variables = "hourly=temperature_2m,relative_humidity_2m&timezone=auto";

    public Task<IReadOnlyList<WeatherReading>> GetArchiveAsync(
        double latitude, double longitude, DateOnly from, DateOnly to, CancellationToken cancellationToken) =>
        FetchAsync(string.Create(CultureInfo.InvariantCulture,
            $"https://archive-api.open-meteo.com/v1/archive?{Point(latitude, longitude)}&start_date={from:yyyy-MM-dd}&end_date={to:yyyy-MM-dd}&{Variables}"),
            WeatherSource.Archive, cancellationToken);

    public Task<IReadOnlyList<WeatherReading>> GetRecentAsync(
        double latitude, double longitude, int pastDays, int forecastDays, CancellationToken cancellationToken) =>
        FetchAsync(string.Create(CultureInfo.InvariantCulture,
            $"https://api.open-meteo.com/v1/forecast?{Point(latitude, longitude)}&past_days={pastDays}&forecast_days={forecastDays + 1}&{Variables}"),
            WeatherSource.Forecast, cancellationToken);

    private static string Point(double latitude, double longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"latitude={latitude:F4}&longitude={longitude:F4}");

    private async Task<IReadOnlyList<WeatherReading>> FetchAsync(string url, WeatherSource source, CancellationToken cancellationToken)
    {
        var response = await http.GetFromJsonAsync<Response>(url, cancellationToken)
            ?? throw new HttpRequestException("Open-Meteo returned an empty body.");
        var readings = new List<WeatherReading>(response.Hourly.Time.Count);
        for (var i = 0; i < response.Hourly.Time.Count; i++)
        {
            if (response.Hourly.Temperature[i] is not { } temperature || response.Hourly.Humidity[i] is not { } humidity) continue;
            var time = DateTime.Parse(response.Hourly.Time[i], CultureInfo.InvariantCulture);
            readings.Add(new WeatherReading(DateOnly.FromDateTime(time), time.Hour, temperature, humidity, source));
        }
        return readings;
    }

    private sealed record Response([property: JsonPropertyName("hourly")] Hourly Hourly);

    private sealed record Hourly(
        [property: JsonPropertyName("time")] IReadOnlyList<string> Time,
        [property: JsonPropertyName("temperature_2m")] IReadOnlyList<double?> Temperature,
        [property: JsonPropertyName("relative_humidity_2m")] IReadOnlyList<double?> Humidity);
}
