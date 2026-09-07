using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class SamplingPointConfiguration : IEntityTypeConfiguration<SamplingPoint>
{
    public void Configure(EntityTypeBuilder<SamplingPoint> builder)
    {
        builder.ToTable("sampling_points");

        builder.HasKey(point => point.Id);
        builder.Property(point => point.Id)
            .HasConversion(id => id.Value, value => new SamplingPointId(value))
            .ValueGeneratedNever();

        builder.Property(point => point.SamplingPlanId)
            .HasConversion(id => id.Value, value => new SamplingPlanId(value))
            .IsRequired();

        builder.Property(point => point.Sequence).IsRequired();

        builder.Property(point => point.Location)
            .HasColumnName("location")
            .HasColumnType($"geography(Point,{Srid.Wgs84})")
            .IsRequired();

        // A ordem da caminhada e unica dentro do plano. O agregado ja numera em sequencia; o indice
        // e a garantia de verdade, e serve de caminho para ler a malha ja ordenada.
        builder.HasIndex(point => new { point.SamplingPlanId, point.Sequence })
            .IsUnique()
            .HasDatabaseName("ix_sampling_points_sequence_per_plan");

        // GiST sobre a coluna geography. Na Fase 4 a coleta em campo pergunta "qual o ponto mais
        // proximo de onde estou", e sem o indice isso vira varredura de todos os pontos da fazenda.
        builder.HasIndex(point => point.Location)
            .HasMethod("gist")
            .HasDatabaseName("ix_sampling_points_location_gist");

        builder.Ignore(point => point.Coordinate);
    }
}
