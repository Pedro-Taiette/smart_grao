using System.Net.Http.Json;
using System.Text.Json;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;

namespace SmartGrao.Infrastructure.Analytics;

internal sealed class AnalyticsClient(HttpClient http) : IAnalyticsClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<IReadOnlyList<SatellitePassResult>> GetSatellitePassesAsync(
        GeoJsonPolygon polygon, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var request = new { polygon = new { type = polygon.Type, coordinates = polygon.Coordinates }, start, end };
        using var response = await http.PostAsJsonAsync("satellite/passes", request, Json, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Response>(Json, cancellationToken)
            ?? throw new HttpRequestException("Analytics returned an empty body.");
        return body.Passes;
    }

    private sealed record Response(IReadOnlyList<SatellitePassResult> Passes);
}
