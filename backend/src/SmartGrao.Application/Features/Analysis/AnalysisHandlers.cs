using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Analysis;
using SmartGrao.Domain.Photos;

namespace SmartGrao.Application.Features.Analysis;

public sealed record AssessmentViewModel(
    Guid Id, Guid PhotoId, AssessmentSource Source, LeafLabel Label, double? Confidence,
    double? CercosporioseProbability, double? HealthyProbability, double? OtherSpotsProbability,
    string? ModelVersion, bool HasHeatmap, Guid? ReviewOf, string? Reviewer, string? Notes, DateTimeOffset AssessedAt)
{
    public static AssessmentViewModel From(Assessment a) => new(
        a.Id.Value, a.PhotoId.Value, a.Source, a.Label, a.Confidence, a.CercosporioseProbability, a.HealthyProbability,
        a.OtherSpotsProbability, a.ModelVersion, a.HeatmapKey is not null, a.ReviewOf?.Value, a.Reviewer, a.Notes, a.AssessedAt);
}

public sealed record AnalysisJobViewModel(Guid Id, Guid PhotoId, AnalysisJobStatus Status, int Attempts, DateTimeOffset NextAttemptAt, string? LastError)
{
    public static AnalysisJobViewModel From(AnalysisJob job) =>
        new(job.Id.Value, job.PhotoId.Value, job.Status, job.Attempts, job.NextAttemptAt, job.LastError);
}

/// <summary>Processa uma tarefa ja reservada pelo worker.</summary>
public sealed class ProcessAnalysisJobCommandHandler(ISmartGraoDbContext db, IPhotoStorage storage, IVisionAnalyzer analyzer)
{
    public async Task HandleAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var id = new AnalysisJobId(jobId);
        var job = await db.AnalysisJobs.FirstAsync(x => x.Id == id, cancellationToken);
        try
        {
            var photo = await db.Photos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.PhotoId, cancellationToken)
                ?? throw new InvalidOperationException("Photo not found.");
            await using var image = await storage.OpenAsync(photo.StorageKey, cancellationToken)
                ?? throw new InvalidOperationException("Photo file not found.");
            var result = await analyzer.AnalyzeAsync(image, photo.ContentType, cancellationToken);
            string? heatmapKey = null;
            if (result.HeatmapPng is { Length: > 0 } png)
            {
                heatmapKey = $"{photo.FieldId.Value:N}/{photo.Id.Value:N}.{Guid.CreateVersion7():N}.heatmap.png";
                using var heatmap = new MemoryStream(png);
                await storage.SaveAsync(heatmapKey, heatmap, cancellationToken);
            }
            db.Assessments.Add(Assessment.FromModel(photo.Id, result.ModelVersion, result.Cercosporiose, result.Healthy,
                result.OtherSpots, heatmapKey, DateTimeOffset.UtcNow));
            job.Succeed(DateTimeOffset.UtcNow);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Fail(exception.Message, DateTimeOffset.UtcNow);
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class RequeueAnalysisCommandHandler(ISmartGraoDbContext db)
{
    public async Task<AnalysisJobViewModel> HandleAsync(Guid photoId, CancellationToken cancellationToken = default)
    {
        var id = new PhotoId(photoId);
        if (!await db.Photos.AnyAsync(x => x.Id == id, cancellationToken))
            throw new DomainException(SmartGraoErrors.Photo.NotFound);
        var job = await db.AnalysisJobs.Where(x => x.PhotoId == id).OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (job is null || job.Status == AnalysisJobStatus.Done)
        {
            job = AnalysisJob.For(id, DateTimeOffset.UtcNow);
            db.AnalysisJobs.Add(job);
        }
        else
        {
            job.Retry(DateTimeOffset.UtcNow);
        }
        await db.SaveChangesAsync(cancellationToken);
        return AnalysisJobViewModel.From(job);
    }
}

public sealed class GetAnalysisJobsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<AnalysisJobViewModel>> HandleAsync(AnalysisJobStatus? status, CancellationToken cancellationToken = default)
    {
        var query = db.AnalysisJobs.AsNoTracking();
        if (status is { } value) query = query.Where(x => x.Status == value);
        var jobs = await query.OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(cancellationToken);
        return jobs.Select(AnalysisJobViewModel.From).ToList();
    }
}

public sealed class GetHeatmapQueryHandler(ISmartGraoDbContext db, IPhotoStorage storage)
{
    public async Task<Stream> HandleAsync(Guid assessmentId, CancellationToken cancellationToken = default)
    {
        var id = new AssessmentId(assessmentId);
        var assessment = await db.Assessments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Analysis.AssessmentNotFound);
        return assessment.HeatmapKey is null
            ? throw new DomainException(SmartGraoErrors.Analysis.AssessmentNotFound)
            : await storage.OpenAsync(assessment.HeatmapKey, cancellationToken)
              ?? throw new DomainException(SmartGraoErrors.Analysis.AssessmentNotFound);
    }
}
