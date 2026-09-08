using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Fields.Events;

namespace SmartGrao.Application.Features.Sampling;

/// <summary>
/// O contorno do talhao mudou: as malhas geradas para o contorno antigo passam a se declarar
/// defasadas.
/// <para>
/// Marcar, e nao apagar nem regerar. Apagar destruiria o roteiro de uma caminhada que pode ja ter
/// comecado; regerar sozinho trocaria em silencio pontos que alguem talvez ja tenha visitado. O
/// plano fica onde esta, dizendo que envelheceu, e quem decide o que fazer e o produtor.
/// </para>
/// </summary>
public sealed class MarkSamplingPlansOutdatedHandler(ISmartGraoDbContext dbContext)
    : IDomainEventHandler<FieldRedrawnEvent>
{
    public async Task HandleAsync(
        FieldRedrawnEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        // Os que ja estao marcados ficam de fora: sao a maioria depois do segundo redesenho, e
        // recarrega-los so para nao mudar nada seria pagar por linha a cada ajuste de contorno.
        var plans = await dbContext.SamplingPlans
            .Where(plan => plan.FieldId == domainEvent.FieldId && !plan.IsOutdated)
            .ToListAsync(cancellationToken);

        foreach (var plan in plans)
            plan.MarkAsOutdated();
    }
}
