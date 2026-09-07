using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

/// <summary>
/// Apaga o plano de verdade, e nao por desativacao. Diferente do talhao, um plano ainda sem coleta
/// nao carrega historico nenhum — e um roteiro que foi gerado e nao serviu. Os pontos vao junto pela
/// cascata, porque nao existem fora dele.
/// </summary>
public sealed class DeleteSamplingPlanCommandHandler(ISmartGraoDbContext dbContext)
{
    public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var planId = new SamplingPlanId(id);

        var plan = await dbContext.SamplingPlans
            .FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Sampling.NotFound);

        dbContext.SamplingPlans.Remove(plan);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
