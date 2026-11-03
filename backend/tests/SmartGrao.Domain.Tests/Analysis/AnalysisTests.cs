using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Analysis;
using SmartGrao.Domain.Photos;
using Xunit;

namespace SmartGrao.Domain.Tests.Analysis;

public sealed class AnalysisTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 12, 0, 0, TimeSpan.Zero);
    private static readonly PhotoId Photo = new(Guid.NewGuid());

    [Fact]
    public void ModelAssessmentPicksTheMostLikelyClass()
    {
        var assessment = Assessment.FromModel(Photo, "efficientnet_b0-v1", 0.7, 0.1, 0.2, "heat.png", Now);

        Assert.Equal(LeafLabel.Cercosporiose, assessment.Label);
        Assert.Equal(0.7, assessment.Confidence);
        Assert.Equal(AssessmentSource.Model, assessment.Source);
    }

    [Theory]
    [InlineData(0.5, 0.5, 0.5)]
    [InlineData(-0.1, 0.6, 0.5)]
    [InlineData(double.NaN, 0.5, 0.5)]
    public void ProbabilitiesMustBeAValidDistribution(double cercosporiose, double healthy, double other)
    {
        Assert.Throws<DomainException>(() => Assessment.FromModel(Photo, "v1", cercosporiose, healthy, other, null, Now));
    }

    [Fact]
    public void ReviewPointsToTheModelAssessment()
    {
        var model = Assessment.FromModel(Photo, "v1", 0.2, 0.7, 0.1, null, Now);

        var review = Assessment.FromReview(model, LeafLabel.Cercosporiose, " Ana ", "Lesao com halo.", Now);

        Assert.Equal(AssessmentSource.Human, review.Source);
        Assert.Equal(model.Id, review.ReviewOf);
        Assert.Equal(model.PhotoId, review.PhotoId);
        Assert.Equal("Ana", review.Reviewer);
        Assert.Null(review.Confidence);
    }

    [Fact]
    public void AReviewCannotBeReviewed()
    {
        var model = Assessment.FromModel(Photo, "v1", 0.2, 0.7, 0.1, null, Now);
        var review = Assessment.FromReview(model, LeafLabel.Healthy, "Ana", null, Now);

        Assert.Throws<DomainException>(() => Assessment.FromReview(review, LeafLabel.Healthy, "Bia", null, Now));
    }

    [Fact]
    public void FailedJobWaitsLongerEachTimeAndGivesUpOnTheFifth()
    {
        var job = AnalysisJob.For(Photo, Now);

        job.Fail("timeout", Now);
        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(Now.AddSeconds(30), job.NextAttemptAt);

        job.Fail("timeout", Now);
        Assert.Equal(Now.AddSeconds(60), job.NextAttemptAt);

        job.Fail("timeout", Now);
        job.Fail("timeout", Now);
        job.Fail("timeout", Now);
        Assert.Equal(AnalysisJobStatus.Failed, job.Status);
        Assert.Equal(AnalysisJob.MaximumAttempts, job.Attempts);
        Assert.Equal("timeout", job.LastError);
    }

    [Fact]
    public void RetryPutsAFailedJobBackInTheQueue()
    {
        var job = AnalysisJob.For(Photo, Now);
        for (var i = 0; i < AnalysisJob.MaximumAttempts; i++) job.Fail("boom", Now);

        job.Retry(Now.AddHours(1));

        Assert.Equal(AnalysisJobStatus.Pending, job.Status);
        Assert.Equal(0, job.Attempts);
        Assert.Null(job.LastError);
    }
}
