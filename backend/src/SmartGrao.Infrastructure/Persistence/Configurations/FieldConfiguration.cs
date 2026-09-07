using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.ToTable("fields");

        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id)
            .HasConversion(id => id.Value, value => new FieldId(value))
            .ValueGeneratedNever();

        builder.Property(field => field.FarmId)
            .HasConversion(id => id.Value, value => new FarmId(value))
            .IsRequired();

        builder.Property(field => field.Name)
            .HasMaxLength(Field.MaximumNameLength)
            .IsRequired();

        // Enum como texto: legivel direto no banco e imune a alguem reordenar os membros — com
        // inteiros, inserir uma cultura no meio do enum reescreveria o significado das linhas ja
        // gravadas.
        builder.Property(field => field.Crop)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(field => field.Geometry)
            .HasColumnName("boundary")
            .HasColumnType($"geography(Polygon,{Srid.Wgs84})")
            .IsRequired();

        // 12,4 cobre de mil metros quadrados ate a maior fazenda imaginavel com precisao de metro
        // quadrado. decimal e nao double: area entra em relatorio e em conta de insumo, e o
        // arredondamento binario apareceria somado talhao a talhao.
        builder.Property(field => field.AreaHectares)
            .HasPrecision(12, 4)
            .IsRequired();

        builder.Property(field => field.PerimeterMeters).IsRequired();
        builder.Property(field => field.Active).IsRequired();

        builder.Property(field => field.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(field => field.UpdatedAt).HasColumnType("timestamp with time zone");

        builder.HasOne<Farm>()
            .WithMany()
            .HasForeignKey(field => field.FarmId)
            .OnDelete(DeleteBehavior.Restrict);

        // Nome unico por fazenda. O caso de uso ja checa para devolver um 409 com codigo traduzivel;
        // este indice e a garantia de verdade, a que duas requisicoes simultaneas nao escapam.
        builder.HasIndex(field => new { field.FarmId, field.Name })
            .IsUnique()
            .HasDatabaseName("ix_fields_name_per_farm");

        // GiST sobre a coluna geography: e o que faz a checagem de sobreposicao (e, na Fase 3, o
        // recorte da malha amostral) consultar poucos candidatos em vez de varrer a tabela.
        builder.HasIndex(field => field.Geometry)
            .HasMethod("gist")
            .HasDatabaseName("ix_fields_boundary_gist");

        builder.Ignore(field => field.DomainEvents);
        builder.Ignore(field => field.Boundary);
    }
}
