using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

internal sealed class SamplingPlanConfiguration : IEntityTypeConfiguration<SamplingPlan>
{
    public void Configure(EntityTypeBuilder<SamplingPlan> builder)
    {
        builder.ToTable("sampling_plans");

        builder.HasKey(plan => plan.Id);
        builder.Property(plan => plan.Id)
            .HasConversion(id => id.Value, value => new SamplingPlanId(value))
            .ValueGeneratedNever();

        builder.Property(plan => plan.FieldId)
            .HasConversion(id => id.Value, value => new FieldId(value))
            .IsRequired();

        // Enum como texto, pela mesma razao da cultura do talhao: legivel no banco e imune a alguem
        // reordenar os membros. Aqui pesa mais ainda — o modo e a chave para interpretar todo o
        // resto da linha, porque bordadura zero significa coisas opostas nos dois modos.
        builder.Property(plan => plan.Mode)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(plan => plan.SpacingMeters).IsRequired();
        builder.Property(plan => plan.EdgeBufferMeters).IsRequired();

        // Nulo no modo Mapeamento, onde a pergunta nao tem numero alvo. A coluna anulavel e o que
        // distingue "nao se aplica" de "alvo zero".
        builder.Property(plan => plan.TargetPointCount);

        builder.Property(plan => plan.FieldAreaHectares)
            .HasPrecision(12, 4)
            .IsRequired();

        builder.Property(plan => plan.SubdivisionRecommended).IsRequired();

        builder.Property(plan => plan.CreatedAt).HasColumnType("timestamp with time zone");
        builder.Property(plan => plan.UpdatedAt).HasColumnType("timestamp with time zone");

        // Restrict e nao Cascade: apagar um talhao nao pode levar junto o historico de amostragem.
        // E a mesma razao de o talhao ser desativado em vez de excluido.
        builder.HasOne<Field>()
            .WithMany()
            .HasForeignKey(plan => plan.FieldId)
            .OnDelete(DeleteBehavior.Restrict);

        // Os pontos so entram pelo agregado. Sem campo de apoio o EF exigiria um setter na colecao
        // publica, que abriria a malha para alteracao de fora.
        builder.Metadata
            .FindNavigation(nameof(SamplingPlan.Points))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(plan => plan.Points)
            .WithOne()
            .HasForeignKey(point => point.SamplingPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // "Planos deste talhao, mais recente primeiro" e a consulta da tela. Sem o indice ela vira
        // varredura da tabela inteira assim que a safra acumular amostragens semanais.
        builder.HasIndex(plan => new { plan.FieldId, plan.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("ix_sampling_plans_field_recent_first");

        builder.Ignore(plan => plan.DomainEvents);
        builder.Ignore(plan => plan.PointCount);
        builder.Ignore(plan => plan.FallsShortOfTarget);
    }
}
