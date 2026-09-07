using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Infrastructure.Persistence;

/// <summary>
/// A unica porta de saida para o PostgreSQL/PostGIS, e tambem a unidade de trabalho: o EF Core ja
/// rastreia as alteracoes e as confirma numa transacao, entao nao ha um segundo objeto envolvendo
/// o primeiro.
/// </summary>
public sealed class SmartGraoDbContext(DbContextOptions<SmartGraoDbContext> options)
    : DbContext(options), ISmartGraoDbContext
{
    public DbSet<Farm> Farms => Set<Farm>();

    public DbSet<Field> Fields => Set<Field>();

    public DbSet<SamplingPlan> SamplingPlans => Set<SamplingPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Declara a extensao no modelo para que a migration emita `CREATE EXTENSION postgis`. No
        // Neon a extensao precisa ser habilitada uma vez por banco; sem esta linha a primeira
        // migration falharia ao criar uma coluna de um tipo que ainda nao existe.
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SmartGraoDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var affected = await base.SaveChangesAsync(cancellationToken);

        // Os eventos so sao limpos depois do commit. Enquanto o despachante nao existe (Fase 3),
        // deixa-los acumular na instancia rastreada faria um agregado relido dentro do mesmo escopo
        // reapresentar eventos ja processados.
        foreach (var entry in ChangeTracker.Entries<IHasDomainEvents>())
            entry.Entity.ClearDomainEvents();

        return affected;
    }
}
