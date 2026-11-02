using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using SmartGrao.Application.Abstractions;

namespace SmartGrao.Infrastructure.Analytics;

/// <summary>Resultado pseudoaleatorio, estavel por foto, com versao "simulado". Nao e diagnostico.</summary>
internal sealed class SimulatedVisionAnalyzer : IVisionAnalyzer
{
    public const string Version = "simulado-0";

    public async Task<VisionResult> AnalyzeAsync(Stream image, string contentType, CancellationToken cancellationToken)
    {
        var hash = await SHA256.HashDataAsync(image, cancellationToken);
        var a = hash[0] + 1d;
        var b = hash[1] + 1d;
        var c = hash[2] + 1d;
        var total = a + b + c;
        return new VisionResult(Version, a / total, b / total, c / total, null);
    }
}

/// <summary>O modelo ONNX servido por analytics/service (POST /vision/classify).</summary>
internal sealed class ServiceVisionAnalyzer(HttpClient http) : IVisionAnalyzer
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

    public async Task<VisionResult> AnalyzeAsync(Stream image, string contentType, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent();
        var file = new StreamContent(image);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "file", "leaf");
        using var response = await http.PostAsync("vision/classify", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<Response>(Json, cancellationToken)
            ?? throw new HttpRequestException("Vision service returned an empty body.");
        return new VisionResult(
            body.ModelVersion, body.Probabilities.GetValueOrDefault("cercosporiose"), body.Probabilities.GetValueOrDefault("saudavel"),
            body.Probabilities.GetValueOrDefault("outras_manchas"),
            body.HeatmapPng is null ? null : Convert.FromBase64String(body.HeatmapPng));
    }

    private sealed record Response(string ModelVersion, Dictionary<string, double> Probabilities, string? HeatmapPng);
}
