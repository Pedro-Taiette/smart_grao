using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Analysis;
using SmartGrao.Domain.Photos;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("assessments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new AssessmentId(x)).ValueGeneratedNever();
        builder.Property(x => x.PhotoId).HasConversion(x => x.Value, x => new PhotoId(x));
        builder.Property(x => x.ReviewOf).HasConversion(x => x!.Value.Value, x => new AssessmentId(x));
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.Label).HasConversion<string>().HasMaxLength(24);
        builder.Property(x => x.ModelVersion).HasMaxLength(60);
        builder.Property(x => x.HeatmapKey).HasMaxLength(200);
        builder.Property(x => x.Reviewer).HasMaxLength(120);
        builder.Property(x => x.Notes).HasMaxLength(Assessment.MaximumNotesLength);
        builder.HasOne<Photo>().WithMany().HasForeignKey(x => x.PhotoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Assessment>().WithMany().HasForeignKey(x => x.ReviewOf).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PhotoId, x.AssessedAt });
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class AnalysisJobConfiguration : IEntityTypeConfiguration<AnalysisJob>
{
    public void Configure(EntityTypeBuilder<AnalysisJob> builder)
    {
        builder.ToTable("analysis_jobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new AnalysisJobId(x)).ValueGeneratedNever();
        builder.Property(x => x.PhotoId).HasConversion(x => x.Value, x => new PhotoId(x));
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.LastError).HasMaxLength(AnalysisJob.MaximumErrorLength);
        builder.HasOne<Photo>().WithMany().HasForeignKey(x => x.PhotoId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt }).HasDatabaseName("ix_analysis_jobs_queue");
        builder.HasIndex(x => x.PhotoId);
        builder.Ignore(x => x.DomainEvents);
    }
}
