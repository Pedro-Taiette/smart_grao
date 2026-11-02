using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Risk;

public enum WeatherSource
{
    Forecast = 0,
    Archive,
    Station,
}

/// <summary>Uma hora de clima no centroide do talhao, em hora local.</summary>
public sealed class WeatherHour
{
    private WeatherHour() { }

    public FieldId FieldId { get; private set; }

    public DateOnly Date { get; private set; }

    public int Hour { get; private set; }

    public double TemperatureC { get; private set; }

    public double RelativeHumidity { get; private set; }

    public WeatherSource Source { get; private set; }

    public DateTimeOffset RetrievedAt { get; private set; }

    public static WeatherHour Create(
        FieldId fieldId, DateOnly date, int hour, double temperatureC, double relativeHumidity,
        WeatherSource source, DateTimeOffset retrievedAt)
    {
        if (hour is < 0 or > 23 || temperatureC is < -50 or > 60 || relativeHumidity is < 0 or > 100
            || !Enum.IsDefined(source))
            throw new DomainException(SmartGraoErrors.Risk.InvalidWeather);
        return new WeatherHour
        {
            FieldId = fieldId, Date = date, Hour = hour, TemperatureC = temperatureC,
            RelativeHumidity = relativeHumidity, Source = source, RetrievedAt = retrievedAt,
        };
    }
}
