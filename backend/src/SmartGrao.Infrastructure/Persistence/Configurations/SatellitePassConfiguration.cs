using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Risk;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class SatellitePassConfiguration : IEntityTypeConfiguration<SatellitePass>
{
    public void Configure(EntityTypeBuilder<SatellitePass> builder)
    {
        builder.ToTable("satellite_passes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SatellitePassId(x)).ValueGeneratedNever();
        builder.Property(x => x.FieldId).HasConversion(x => x.Value, x => new FieldId(x));
        builder.Property(x => x.SceneId).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.OwnsOne(x => x.Ndvi, stats =>
        {
            stats.Property(x => x.Mean).HasColumnName("ndvi_mean");
            stats.Property(x => x.Median).HasColumnName("ndvi_median");
            stats.Property(x => x.P10).HasColumnName("ndvi_p10");
            stats.Property(x => x.P90).HasColumnName("ndvi_p90");
        });
        builder.OwnsOne(x => x.Ndre, stats =>
        {
            stats.Property(x => x.Mean).HasColumnName("ndre_mean");
            stats.Property(x => x.Median).HasColumnName("ndre_median");
            stats.Property(x => x.P10).HasColumnName("ndre_p10");
            stats.Property(x => x.P90).HasColumnName("ndre_p90");
        });
        builder.OwnsMany(x => x.Zones, zone =>
        {
            zone.ToTable("anomaly_zones");
            zone.WithOwner().HasForeignKey("satellite_pass_id");
            zone.HasKey("satellite_pass_id", nameof(AnomalyZone.Index));
            zone.Property(x => x.Index).ValueGeneratedNever();
            zone.Property(x => x.Geometry).HasColumnType($"geography(Polygon,{Srid.Wgs84})");
        });
        builder.Navigation(x => x.Zones).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => new { x.FieldId, x.Date }).IsUnique().HasDatabaseName("ix_satellite_passes_field_date");
        builder.Ignore(x => x.DomainEvents);
    }
}
