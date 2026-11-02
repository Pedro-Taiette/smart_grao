using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Features.Risk;
using SmartGrao.Domain.Analysis;
using SmartGrao.Domain.Photos;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Application.Features.Analysis;

public sealed record ModelSummaryViewModel(
    string? Name, int? N, double? Accuracy, double? MacroF1, double? RecallCercosporiose, double? PrecisionCercosporiose, double? Ece);

public sealed record ModelCardViewModel(
    string ModelVersion, string? Split, ModelSummaryViewModel Test, IReadOnlyList<string> ConfusionLabels,
    IReadOnlyList<IReadOnlyList<int>> ConfusionMatrix, IReadOnlyList<ModelSummaryViewModel> ByDomain,
    IReadOnlyList<ModelSummaryViewModel> ByDataset, OutOfDomainViewModel? OutOfDomain);

public sealed record OutOfDomainViewModel(string Holdout, ModelSummaryViewModel Test, double? ValidationMacroF1);

public sealed record ReviewMetricsViewModel(
    int Photos, int Analyzed, int Reviewed, int SimulatedExcluded, double? RecallCercosporiose, double? PrecisionCercosporiose,
    double? Agreement, IReadOnlyList<LeafLabel> ConfusionLabels, IReadOnlyList<IReadOnlyList<int>> ConfusionMatrix);

public sealed record LeadTimeViewModel(
    Guid CultivationId, Guid FieldId, string FieldName, DateOnly ConfirmedOn, DateOnly? FirstHighRisk, int? ClimateLeadDays,
    DateOnly? FirstZone, int? SatelliteLeadDays);

public sealed record CalibrationViewModel(
    ModelCardViewModel? Model, string? ModelUnavailableReason, ReviewMetricsViewModel Reviews, IReadOnlyList<LeadTimeViewModel> LeadTimes);

/// <summary>
/// O sistema medindo o quanto acerta: o modelo no conjunto de teste, o modelo contra as revisoes, e a
/// antecedencia de clima e satelite em relacao a primeira confirmacao na folha.
/// </summary>
public sealed class GetCalibrationQueryHandler(ISmartGraoDbContext db, IAnalyticsClient analytics)
{
    /// <summary>So conta como aviso previo o que veio no maximo isso antes da confirmacao.</summary>
    public const int LeadWindowDays = 45;

    public async Task<CalibrationViewModel> HandleAsync(CancellationToken cancellationToken = default)
    {
        ModelCardViewModel? model = null;
        string? reason = null;
        try
        {
            model = await analytics.GetModelCardAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            reason = exception.Message;
        }

        var pairs = await ReviewedPairsAsync(cancellationToken);
        return new CalibrationViewModel(model, reason, await ReviewMetricsAsync(pairs, cancellationToken), await LeadTimesAsync(pairs, cancellationToken));
    }

    internal sealed record Pair(Photo Photo, Assessment Model, Assessment? Review);

    internal async Task<IReadOnlyList<Pair>> ReviewedPairsAsync(CancellationToken cancellationToken)
    {
        var photos = await db.Photos.AsNoTracking().ToListAsync(cancellationToken);
        var assessments = (await db.Assessments.AsNoTracking().ToListAsync(cancellationToken)).ToLookup(x => x.PhotoId);
        var pairs = new List<Pair>();
        foreach (var photo in photos)
        {
            var model = assessments[photo.Id].Where(x => x.Source == AssessmentSource.Model).MaxBy(x => x.AssessedAt);
            if (model is null) continue;
            var review = assessments[photo.Id].Where(x => x.Source == AssessmentSource.Human && x.ReviewOf == model.Id).MaxBy(x => x.AssessedAt);
            pairs.Add(new Pair(photo, model, review));
        }
        return pairs;
    }

    private async Task<ReviewMetricsViewModel> ReviewMetricsAsync(IReadOnlyList<Pair> pairs, CancellationToken cancellationToken)
    {
        var total = await db.Photos.CountAsync(cancellationToken);
        var real = pairs.Where(x => x.Model.ModelVersion?.StartsWith("simulado", StringComparison.Ordinal) != true).ToList();
        var reviewed = real.Where(x => x.Review is not null).ToList();
        LeafLabel[] labels = [LeafLabel.Cercosporiose, LeafLabel.Healthy, LeafLabel.OtherSpots];
        var matrix = labels.Select(truth => (IReadOnlyList<int>)labels.Select(predicted =>
            reviewed.Count(x => x.Review!.Label == truth && x.Model.Label == predicted)).ToList()).ToList();
        var truePositive = reviewed.Count(x => x.Review!.Label == LeafLabel.Cercosporiose && x.Model.Label == LeafLabel.Cercosporiose);
        var actual = reviewed.Count(x => x.Review!.Label == LeafLabel.Cercosporiose);
        var predicted = reviewed.Count(x => x.Model.Label == LeafLabel.Cercosporiose);
        return new ReviewMetricsViewModel(
            total, pairs.Count, reviewed.Count, pairs.Count - real.Count,
            actual == 0 ? null : (double)truePositive / actual,
            predicted == 0 ? null : (double)truePositive / predicted,
            reviewed.Count == 0 ? null : (double)reviewed.Count(x => x.Review!.Label == x.Model.Label) / reviewed.Count,
            labels, matrix);
    }

    private async Task<IReadOnlyList<LeadTimeViewModel>> LeadTimesAsync(IReadOnlyList<Pair> pairs, CancellationToken cancellationToken)
    {
        var confirmations = pairs.Where(x => x.Review?.Label == LeafLabel.Cercosporiose && !x.Photo.IsSimulated)
            .GroupBy(x => x.Photo.CultivationId)
            .Select(g => (Cultivation: g.Key, ConfirmedOn: g.Min(x => DateOnly.FromDateTime(x.Photo.TakenAt.ToOffset(TimeSpan.FromHours(-3)).DateTime))))
            .ToList();
        var result = new List<LeadTimeViewModel>();
        foreach (var (cultivationId, confirmedOn) in confirmations)
        {
            var cultivation = await db.Cultivations.AsNoTracking().Include(x => x.Stages).FirstAsync(x => x.Id == cultivationId, cancellationToken);
            var fieldName = await db.Fields.Where(x => x.Id == cultivation.FieldId).Select(x => x.Name).FirstAsync(cancellationToken);
            var from = confirmedOn.AddDays(-LeadWindowDays) < cultivation.PlantedOn ? cultivation.PlantedOn : confirmedOn.AddDays(-LeadWindowDays);
            var hours = await RiskProjection.LoadHoursAsync(db, cultivation.FieldId, from, confirmedOn, cancellationToken);
            var firstHigh = CercosporaRiskIndex.Compute(hours, from, confirmedOn, cultivation.RiskFactors)
                .FirstOrDefault(x => x.Level == RiskLevel.High)?.Date;
            var firstZone = await db.SatellitePasses.AsNoTracking()
                .Where(x => x.FieldId == cultivation.FieldId && x.Date >= from && x.Date <= confirmedOn && x.Zones.Any())
                .OrderBy(x => x.Date).Select(x => (DateOnly?)x.Date).FirstOrDefaultAsync(cancellationToken);
            result.Add(new LeadTimeViewModel(cultivation.Id.Value, cultivation.FieldId.Value, fieldName, confirmedOn,
                firstHigh, firstHigh is { } high ? confirmedOn.DayNumber - high.DayNumber : null,
                firstZone, firstZone is { } zone ? confirmedOn.DayNumber - zone.DayNumber : null));
        }
        return result;
    }
}

/// <summary>Fotos revisadas com rotulo e rastreabilidade: o conjunto de treino da proxima versao.</summary>
public sealed class ExportReviewedPhotosQueryHandler(ISmartGraoDbContext db, IAnalyticsClient analytics)
{
    public async Task<string> HandleAsync(CancellationToken cancellationToken = default)
    {
        var pairs = await new GetCalibrationQueryHandler(db, analytics).ReviewedPairsAsync(cancellationToken);
        var fields = await db.Fields.AsNoTracking().ToDictionaryAsync(x => x.Id, x => x.Name, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("photo_id,content_path,field,field_id,cultivation_id,inspection_id,observation_id,organ,growth_stage,taken_at,latitude,longitude,source,simulated,model_version,model_label,model_confidence,review_label,reviewer");
        foreach (var pair in pairs.Where(x => x.Review is not null))
        {
            var photo = pair.Photo;
            string[] row =
            [
                photo.Id.Value.ToString(), $"/api/photos/{photo.Id.Value}/content", fields.GetValueOrDefault(photo.FieldId, ""),
                photo.FieldId.Value.ToString(), photo.CultivationId.Value.ToString(), photo.InspectionId?.Value.ToString() ?? "",
                photo.ObservationId?.Value.ToString() ?? "", photo.Organ.ToString(), photo.GrowthStage ?? "",
                photo.TakenAt.ToString("O", CultureInfo.InvariantCulture),
                photo.Location?.Y.ToString(CultureInfo.InvariantCulture) ?? "", photo.Location?.X.ToString(CultureInfo.InvariantCulture) ?? "",
                photo.Source.ToString(), photo.IsSimulated ? "true" : "false", pair.Model.ModelVersion ?? "", pair.Model.Label.ToString(),
                pair.Model.Confidence?.ToString("0.0000", CultureInfo.InvariantCulture) ?? "", pair.Review!.Label.ToString(), pair.Review.Reviewer ?? "",
            ];
            csv.AppendLine(string.Join(',', row.Select(Escape)));
        }
        return csv.ToString();
    }

    private static string Escape(string value) =>
        value.IndexOfAny([',', '"', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
