using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("people");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new PersonId(x)).ValueGeneratedNever();
        builder.Property(x => x.FarmId).HasConversion(x => x.Value, x => new FarmId(x));
        builder.Property(x => x.Name).HasMaxLength(Person.MaximumNameLength).IsRequired();
        builder.Property(x => x.Role).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Active).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => x.FarmId).OnDelete(DeleteBehavior.Restrict);
        // Sem unicidade no nome: duas pessoas homonimas na mesma equipe existem, e recusar o
        // cadastro da segunda seria inventar uma regra que o mundo nao tem.
        builder.HasIndex(x => new { x.FarmId, x.Name });
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class InspectionConfiguration : IEntityTypeConfiguration<Inspection>
{
    public void Configure(EntityTypeBuilder<Inspection> builder)
    {
        builder.ToTable("inspections");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new InspectionId(x)).ValueGeneratedNever();
        builder.Property(x => x.FieldId).HasConversion(x => x.Value, x => new FieldId(x));
        builder.Property(x => x.CultivationId).HasConversion(x => x.Value, x => new CultivationId(x));
        builder.Property(x => x.SamplingPlanId).HasConversion(x => x.Value, x => new SamplingPlanId(x));
        builder.Property(x => x.ProtocolId).HasConversion(x => x.Value, x => new ProtocolId(x));
        builder.Property(x => x.ResponsibleId).HasConversion(x => x.Value, x => new PersonId(x));
        builder.Property(x => x.Crop).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(x => x.CancellationReason).HasMaxLength(Inspection.MaximumReasonLength);
        builder.Property(x => x.StartedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CompletedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamp with time zone");

        // Restrict em tudo: a vistoria e o registro de que alguem esteve naquele talhao naquele dia,
        // e apagar talhao, cultivo, plano, protocolo ou pessoa nao pode levar esse registro junto.
        builder.HasOne<Field>().WithMany().HasForeignKey(x => x.FieldId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cultivation>().WithMany().HasForeignKey(x => x.CultivationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SamplingPlan>().WithMany().HasForeignKey(x => x.SamplingPlanId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Protocol>().WithMany().HasForeignKey(x => x.ProtocolId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => x.ResponsibleId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Observations).WithOne()
            .HasForeignKey(x => x.InspectionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Observations).UsePropertyAccessMode(PropertyAccessMode.Field);

        // "Vistorias deste cultivo, mais recente primeiro" e a consulta da tela; a agenda de uma
        // pessoa e a outra. Sem os indices, as duas viram varredura assim que a safra acumular
        // vistorias semanais.
        builder.HasIndex(x => new { x.CultivationId, x.ScheduledFor }).IsDescending(false, true);
        builder.HasIndex(x => new { x.ResponsibleId, x.Status });

        // Iniciar e concluir em duas maos ao mesmo tempo deixaria a visita num estado que nenhuma
        // das duas pediu.
        builder.Property<uint>("RowVersion").IsRowVersion();

        builder.Ignore(x => x.DomainEvents);
        builder.Ignore(x => x.IsFinished);
        builder.Ignore(x => x.VisitedPointCount);
        builder.Ignore(x => x.OffPlanObservationCount);
    }
}

internal sealed class ObservationConfiguration : IEntityTypeConfiguration<Observation>
{
    public void Configure(EntityTypeBuilder<Observation> builder)
    {
        builder.ToTable("observations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ObservationId(x)).ValueGeneratedNever();
        builder.Property(x => x.InspectionId).HasConversion(x => x.Value, x => new InspectionId(x));
        builder.Property(x => x.SamplingPointId).HasConversion(x => x!.Value.Value, x => new SamplingPointId(x));
        builder.Property(x => x.RecordedAt).HasColumnType("timestamp with time zone").IsRequired();

        // geography(Point,4326), como a malha: "o que foi observado perto daqui" precisa virar
        // ST_DWithin no banco, e nao varredura na memoria da aplicacao.
        builder.Property(x => x.Location)
            .HasColumnType($"geography(Point,{Srid.Wgs84})")
            .IsRequired();

        builder.Property(x => x.AccuracyMeters);
        builder.Property(x => x.GrowthStage).HasMaxLength(32);
        builder.Property(x => x.Notes).HasMaxLength(Observation.MaximumNotesLength);

        builder.HasOne<SamplingPoint>().WithMany()
            .HasForeignKey(x => x.SamplingPointId).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Counts).WithOne()
            .HasForeignKey(x => x.ObservationId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Counts).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Um ponto da malha e visitado uma vez por vistoria. Os nulos ficam distintos entre si — que
        // e o padrao do PostgreSQL e exatamente o que se quer aqui: ocorrencias fora da malha sao
        // varias por visita, de proposito.
        builder.HasIndex(x => new { x.InspectionId, x.SamplingPointId })
            .IsUnique()
            .HasDatabaseName("ix_observations_point_per_inspection");

        builder.Ignore(x => x.Coordinate);
        builder.Ignore(x => x.IsOffPlan);
        builder.Ignore(x => x.HasPoorAccuracy);
        builder.Ignore(x => x.Detections);
    }
}

internal sealed class TargetCountConfiguration : IEntityTypeConfiguration<TargetCount>
{
    public void Configure(EntityTypeBuilder<TargetCount> builder)
    {
        builder.ToTable("target_counts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new TargetCountId(x)).ValueGeneratedNever();
        builder.Property(x => x.ObservationId).HasConversion(x => x.Value, x => new ObservationId(x));
        builder.Property(x => x.ProtocolItemId).HasConversion(x => x.Value, x => new ProtocolItemId(x));
        builder.Property(x => x.TargetId).HasConversion(x => x.Value, x => new MonitoringTargetId(x));
        builder.Property(x => x.Unit).HasConversion<string>().HasMaxLength(40);
        builder.Property(x => x.Value).HasPrecision(10, 2);
        builder.Property(x => x.Detected).IsRequired();

        builder.HasOne<ProtocolItem>().WithMany()
            .HasForeignKey(x => x.ProtocolItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MonitoringTarget>().WithMany()
            .HasForeignKey(x => x.TargetId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.ObservationId, x.ProtocolItemId })
            .IsUnique()
            .HasDatabaseName("ix_target_counts_item_per_observation");

        // "Onde este alvo apareceu" e a consulta das fases 5 e 6, e ela filtra por deteccao.
        builder.HasIndex(x => new { x.TargetId, x.Detected });
    }
}
