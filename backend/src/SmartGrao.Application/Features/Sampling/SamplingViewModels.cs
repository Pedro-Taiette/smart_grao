using SmartGrao.Application.Common;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

/// <summary>
/// Pedido de geracao de um plano.
/// <para>
/// O espacamento so faz sentido no modo Mapeamento, e por isso e anulavel: no Monitoramento ele nao
/// e escolhido, e derivado da tabela do MIP-Soja. Mandar espacamento junto com
/// <see cref="SamplingMode.Monitoring"/> e recusado em vez de ignorado — aceitar em silencio faria
/// o cliente acreditar que mandou algo que teve efeito.
/// </para>
/// <para>
/// A bordadura nao aparece aqui. Ela e regra de dominio, com default por modo, e pedi-la ao cliente
/// seria mover uma decisao agronomica para fora do dominio.
/// </para>
/// </summary>
public sealed record GenerateSamplingPlanViewModel(
    Guid FieldId,
    SamplingMode Mode,
    double? SpacingMeters);

/// <summary>Um ponto da malha, na ordem da caminhada.</summary>
public sealed record SamplingPointViewModel(Guid Id, int Sequence, GeoJsonPoint Location)
{
    public static SamplingPointViewModel From(SamplingPoint point)
    {
        ArgumentNullException.ThrowIfNull(point);

        return new SamplingPointViewModel(
            point.Id.Value,
            point.Sequence,
            GeoJsonPoint.From(point.Coordinate));
    }
}

/// <summary>
/// O plano sem os pontos — o que a listagem do talhao mostra. Carregar a malha inteira so para
/// desenhar uma linha de historico traria centenas de coordenadas por linha.
/// </summary>
public sealed record SamplingPlanSummaryViewModel(
    Guid Id,
    Guid FieldId,
    SamplingMode Mode,
    double SpacingMeters,
    double EdgeBufferMeters,
    int? TargetPointCount,
    int PointCount,
    bool FallsShortOfTarget,
    bool SubdivisionRecommended,
    bool IsOutdated,
    decimal FieldAreaHectares,
    DateTimeOffset CreatedAt)
{
    public static SamplingPlanSummaryViewModel From(SamplingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new SamplingPlanSummaryViewModel(
            plan.Id.Value,
            plan.FieldId.Value,
            plan.Mode,
            Math.Round(plan.SpacingMeters, 2),
            Math.Round(plan.EdgeBufferMeters, 2),
            plan.TargetPointCount,
            plan.PointCount,
            plan.FallsShortOfTarget,
            plan.SubdivisionRecommended,
            plan.IsOutdated,
            plan.FieldAreaHectares,
            plan.CreatedAt);
    }
}

/// <summary>O plano com a malha.</summary>
public sealed record SamplingPlanViewModel(
    SamplingPlanSummaryViewModel Plan,
    IReadOnlyList<SamplingPointViewModel> Points)
{
    public static SamplingPlanViewModel From(SamplingPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return new SamplingPlanViewModel(
            SamplingPlanSummaryViewModel.From(plan),
            [.. plan.Points.OrderBy(point => point.Sequence).Select(SamplingPointViewModel.From)]);
    }
}
