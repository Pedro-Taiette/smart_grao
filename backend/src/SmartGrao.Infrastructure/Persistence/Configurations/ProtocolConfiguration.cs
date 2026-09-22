using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class MonitoringTargetConfiguration : IEntityTypeConfiguration<MonitoringTarget>
{
    public void Configure(EntityTypeBuilder<MonitoringTarget> builder)
    {
        builder.ToTable("monitoring_targets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new MonitoringTargetId(x)).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(MonitoringTarget.MaximumCodeLength).IsRequired();
        builder.Property(x => x.CommonName).HasMaxLength(MonitoringTarget.MaximumNameLength).IsRequired();
        builder.Property(x => x.ScientificName).HasMaxLength(MonitoringTarget.MaximumNameLength).IsRequired();
        builder.Property(x => x.Kind).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Crop).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Automation).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("ix_monitoring_targets_code");
        builder.HasIndex(x => new { x.Crop, x.Kind });
        builder.Ignore(x => x.DomainEvents);
        builder.HasData(MonitoringTargetSeed.Rows);
    }
}

internal sealed class ProtocolConfiguration : IEntityTypeConfiguration<Protocol>
{
    public void Configure(EntityTypeBuilder<Protocol> builder)
    {
        builder.ToTable("protocols");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ProtocolId(x)).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(Protocol.MaximumCodeLength).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(Protocol.MaximumNameLength).IsRequired();
        builder.Property(x => x.Crop).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(x => new { x.Code, x.Version }).IsUnique().HasDatabaseName("ix_protocols_code_version");
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.ProtocolId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        // Impede que uma publicacao concorrente congele um protocolo do qual alguem acabou de
        // remover um alvo — depois de publicado nao ha como corrigir no lugar.
        builder.Property<uint>("RowVersion").IsRowVersion();
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class ProtocolItemConfiguration : IEntityTypeConfiguration<ProtocolItem>
{
    public void Configure(EntityTypeBuilder<ProtocolItem> builder)
    {
        builder.ToTable("protocol_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ProtocolItemId(x)).ValueGeneratedNever();
        builder.Property(x => x.ProtocolId).HasConversion(x => x.Value, x => new ProtocolId(x));
        builder.Property(x => x.TargetId).HasConversion(x => x.Value, x => new MonitoringTargetId(x));
        builder.Property(x => x.Organ).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Unit).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Instructions).HasMaxLength(ProtocolItem.MaximumInstructionsLength).IsRequired();
        builder.OwnsOne(x => x.ReferenceLevel, level =>
        {
            level.Property(x => x.Threshold).HasColumnName("reference_threshold").HasPrecision(10, 2);
            level.Property(x => x.Source).HasColumnName("reference_source")
                .HasMaxLength(ReferenceLevel.MaximumSourceLength);
        });
        builder.HasOne<MonitoringTarget>().WithMany().HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TargetId);
    }
}
