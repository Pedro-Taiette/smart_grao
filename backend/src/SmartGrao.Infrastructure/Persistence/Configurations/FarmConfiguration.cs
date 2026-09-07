using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("farms");

        builder.HasKey(farm => farm.Id);
        builder.Property(farm => farm.Id)
            .HasConversion(id => id.Value, value => new FarmId(value))
            .ValueGeneratedNever();

        builder.Property(farm => farm.Name)
            .HasMaxLength(Farm.MaximumNameLength)
            .IsRequired();

        builder.Property(farm => farm.City)
            .HasMaxLength(Farm.MaximumCityLength)
            .IsRequired();

        builder.Property(farm => farm.State)
            .HasMaxLength(2)
            .IsFixedLength()
            .IsRequired();

        // geography e nao geometry: distancias e areas saem em metros sobre o elipsoide, sem que
        // ninguem precise escolher uma projecao local para cada fazenda do pais.
        builder.Property(farm => farm.Headquarters)
            .HasColumnType($"geography(Point,{Srid.Wgs84})");

        builder.Property(farm => farm.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(farm => farm.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasIndex(farm => farm.Name);

        builder.Ignore(farm => farm.DomainEvents);
        builder.Ignore(farm => farm.HeadquartersCoordinate);
    }
}
