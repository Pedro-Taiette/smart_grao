using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.Photos;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("photos");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new PhotoId(x)).ValueGeneratedNever();
        builder.Property(x => x.FieldId).HasConversion(x => x.Value, x => new FieldId(x));
        builder.Property(x => x.CultivationId).HasConversion(x => x.Value, x => new CultivationId(x));
        builder.Property(x => x.InspectionId).HasConversion(x => x!.Value.Value, x => new InspectionId(x));
        builder.Property(x => x.ObservationId).HasConversion(x => x!.Value.Value, x => new ObservationId(x));
        builder.Property(x => x.TargetId).HasConversion(x => x!.Value.Value, x => new MonitoringTargetId(x));
        builder.Property(x => x.Organ).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.GrowthStage).HasMaxLength(32);
        builder.Property(x => x.ContentType).HasMaxLength(32);
        builder.Property(x => x.StorageKey).HasMaxLength(200);
        builder.Property(x => x.Location).HasColumnType($"geography(Point,{Srid.Wgs84})");
        builder.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cultivation>().WithMany().HasForeignKey(x => x.CultivationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Inspection>().WithMany().HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Observation>().WithMany().HasForeignKey(x => x.ObservationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.ObservationId);
        builder.HasIndex(x => new { x.CultivationId, x.TakenAt });
        builder.Ignore(x => x.DomainEvents);
    }
}
