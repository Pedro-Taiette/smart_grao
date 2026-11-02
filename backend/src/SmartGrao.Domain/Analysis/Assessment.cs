using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Photos;

namespace SmartGrao.Domain.Analysis;

public readonly record struct AssessmentId(Guid Value) : IStronglyTypedId
{
    public static AssessmentId New() => new(Guid.CreateVersion7());
}

public enum LeafLabel
{
    Cercosporiose = 0,
    Healthy,
    OtherSpots,
}

public enum AssessmentSource
{
    Model = 0,
    Human,
}

/// <summary>
/// A interpretacao de uma foto, separada da contagem de campo. Uma foto pode ter varias: de modelos
/// diferentes, ou de um modelo e de uma pessoa.
/// </summary>
public sealed class Assessment : AggregateRoot<AssessmentId>
{
    public const int MaximumNotesLength = 500;

    private Assessment() { }

    public PhotoId PhotoId { get; private set; }

    public AssessmentSource Source { get; private set; }

    public LeafLabel Label { get; private set; }

    /// <summary>So do modelo: probabilidade da classe escolhida.</summary>
    public double? Confidence { get; private set; }

    public double? CercosporioseProbability { get; private set; }

    public double? HealthyProbability { get; private set; }

    public double? OtherSpotsProbability { get; private set; }

    public string? ModelVersion { get; private set; }

    public string? HeatmapKey { get; private set; }

    /// <summary>So da pessoa: a avaliacao do modelo que ela confirmou ou corrigiu.</summary>
    public AssessmentId? ReviewOf { get; private set; }

    public string? Reviewer { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset AssessedAt { get; private set; }

    public static Assessment FromModel(
        PhotoId photoId, string modelVersion, double cercosporiose, double healthy, double otherSpots,
        string? heatmapKey, DateTimeOffset assessedAt)
    {
        if (string.IsNullOrWhiteSpace(modelVersion) || modelVersion.Length > 60)
            throw new DomainException(SmartGraoErrors.Analysis.InvalidModelVersion);
        double[] probabilities = [cercosporiose, healthy, otherSpots];
        if (probabilities.Any(p => double.IsNaN(p) || p < 0 || p > 1) || Math.Abs(probabilities.Sum() - 1) > 0.01)
            throw new DomainException(SmartGraoErrors.Analysis.InvalidProbabilities);
        var best = Array.IndexOf(probabilities, probabilities.Max());
        return new Assessment
        {
            Id = AssessmentId.New(), PhotoId = photoId, Source = AssessmentSource.Model, Label = (LeafLabel)best,
            Confidence = probabilities[best], CercosporioseProbability = cercosporiose, HealthyProbability = healthy,
            OtherSpotsProbability = otherSpots, ModelVersion = modelVersion.Trim(), HeatmapKey = heatmapKey,
            AssessedAt = assessedAt.ToUniversalTime(),
        };
    }

    public static Assessment FromReview(Assessment reviewed, LeafLabel label, string reviewer, string? notes, DateTimeOffset assessedAt)
    {
        ArgumentNullException.ThrowIfNull(reviewed);
        if (reviewed.Source != AssessmentSource.Model)
            throw new DomainException(SmartGraoErrors.Analysis.ReviewOfReview);
        if (!Enum.IsDefined(label))
            throw new DomainException(SmartGraoErrors.Analysis.UnknownLabel);
        if (string.IsNullOrWhiteSpace(reviewer) || reviewer.Trim().Length > 120 || notes?.Length > MaximumNotesLength)
            throw new DomainException(SmartGraoErrors.Analysis.InvalidReview);
        return new Assessment
        {
            Id = AssessmentId.New(), PhotoId = reviewed.PhotoId, Source = AssessmentSource.Human, Label = label,
            ReviewOf = reviewed.Id, Reviewer = reviewer.Trim(), Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            AssessedAt = assessedAt.ToUniversalTime(),
        };
    }
}
