using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
{
    public void Configure(EntityTypeBuilder<Season> builder)
    {
        builder.ToTable("seasons");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new SeasonId(x)).ValueGeneratedNever();
        builder.Property(x => x.FarmId).HasConversion(x => x.Value, x => new FarmId(x));
        builder.Property(x => x.Name).HasMaxLength(Season.MaximumNameLength).IsRequired();
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FarmId, x.Name }).IsUnique().HasDatabaseName("ix_seasons_name_per_farm");
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class CultivationConfiguration : IEntityTypeConfiguration<Cultivation>
{
    public void Configure(EntityTypeBuilder<Cultivation> builder)
    {
        builder.ToTable("cultivations", table => table.HasCheckConstraint("ck_cultivation_dates", "ended_on IS NULL OR ended_on >= planted_on"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new CultivationId(x)).ValueGeneratedNever();
        builder.Property(x => x.FieldId).HasConversion(x => x.Value, x => new FieldId(x));
        builder.Property(x => x.SeasonId).HasConversion(x => x.Value, x => new SeasonId(x));
        builder.Property(x => x.Crop).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Cultivar).HasMaxLength(120).IsRequired();
        builder.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Season>().WithMany().HasForeignKey(x => x.SeasonId).OnDelete(DeleteBehavior.Restrict);
        builder.HasAlternateKey(x => new { x.Id, x.FieldId });
        builder.HasIndex(x => new { x.FieldId, x.PlantedOn }).IsDescending(false, true);
        builder.HasMany(x => x.Stages).WithOne().HasForeignKey(x => x.CultivationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Stages).UsePropertyAccessMode(PropertyAccessMode.Field);
        // Prevent a concurrent close and stage insertion from accepting inconsistent dates.
        builder.Property<uint>("Version").IsRowVersion();
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class GrowthStageRecordConfiguration : IEntityTypeConfiguration<GrowthStageRecord>
{
    public void Configure(EntityTypeBuilder<GrowthStageRecord> builder)
    {
        builder.ToTable("growth_stage_records");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new GrowthStageRecordId(x)).ValueGeneratedNever();
        builder.Property(x => x.CultivationId).HasConversion(x => x.Value, x => new CultivationId(x));
        builder.Property(x => x.Stage).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.HasIndex(x => new { x.CultivationId, x.ObservedOn }).IsUnique().HasDatabaseName("ix_stage_date_per_cultivation");
    }
}
