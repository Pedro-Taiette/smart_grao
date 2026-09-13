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
public sealed class SmartGraoDbContext(
    DbContextOptions<SmartGraoDbContext> options,
    IDomainEventDispatcher domainEventDispatcher)
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
        await DispatchDomainEventsAsync(cancellationToken);

        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Entrega os eventos acumulados pelos agregados antes de gravar, para que o que os handlers
    /// escreverem entre na mesma transacao.
    /// <para>
    /// O laco existe porque um handler pode fazer um agregado levantar outro evento. Ele termina
    /// quando ninguem mais tem evento pendente — e nao ha risco de rodar para sempre enquanto os
    /// eventos forem consequencia de mudancas de estado, ja que um agregado so publica de novo se
    /// mudar de novo.
    /// </para>
    /// </summary>
    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            var pending = ChangeTracker.Entries<IHasDomainEvents>()
                .Select(entry => entry.Entity)
                .Where(entity => entity.DomainEvents.Count > 0)
                .ToList();

            if (pending.Count == 0) return;

            var events = pending.SelectMany(entity => entity.DomainEvents).ToList();

            // Limpa antes de entregar: um handler que consulte o mesmo contexto faria o EF
            // reapresentar estes agregados, e sem a limpeza o mesmo evento seria entregue de novo.
            foreach (var entity in pending)
                entity.ClearDomainEvents();

            await domainEventDispatcher.DispatchAsync(events, cancellationToken);
        }
    }
}
