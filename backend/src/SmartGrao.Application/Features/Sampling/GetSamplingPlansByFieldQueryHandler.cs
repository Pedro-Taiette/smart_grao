using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Sampling;

/// <summary>
/// Historico de amostragem do talhao, do mais recente para o mais antigo.
/// <para>
/// Devolve o resumo, sem a malha. O MIP pede amostragem semanal, entao a lista cresce safra afora, e
/// trazer os pontos de cada plano so para desenhar uma linha de historico carregaria centenas de
/// coordenadas por linha. Quem quer a malha pede um plano especifico.
/// </para>
/// </summary>
public sealed class GetSamplingPlansByFieldQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<IReadOnlyList<SamplingPlanSummaryViewModel>> HandleAsync(
        Guid fieldId, CancellationToken cancellationToken = default)
    {
        var id = new FieldId(fieldId);

        var plans = await dbContext.SamplingPlans
            .AsNoTracking()
            .Where(plan => plan.FieldId == id)
            .OrderByDescending(plan => plan.CreatedAt)
            // A contagem vem do banco, e nao da colecao carregada: e o que permite nao trazer os
            // pontos e ainda assim dizer quantos sao.
            .Select(plan => new SamplingPlanSummaryViewModel(
                plan.Id.Value,
                plan.FieldId.Value,
                plan.Mode,
                Math.Round(plan.SpacingMeters, 2),
                Math.Round(plan.EdgeBufferMeters, 2),
                plan.TargetPointCount,
                plan.Points.Count,
                plan.TargetPointCount != null && plan.Points.Count < plan.TargetPointCount,
                plan.SubdivisionRecommended,
                plan.IsOutdated,
                plan.FieldAreaHectares,
                plan.CreatedAt))
            .ToListAsync(cancellationToken);

        return plans;
    }
}
