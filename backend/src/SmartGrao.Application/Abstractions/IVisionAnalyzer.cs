namespace SmartGrao.Application.Abstractions;

public sealed record VisionResult(
    string ModelVersion, double Cercosporiose, double Healthy, double OtherSpots, byte[]? HeatmapPng);

/// <summary>Classifica a foto de uma folha. O simulado comprova a fila antes de o modelo existir.</summary>
public interface IVisionAnalyzer
{
    Task<VisionResult> AnalyzeAsync(Stream image, string contentType, CancellationToken cancellationToken);
}
