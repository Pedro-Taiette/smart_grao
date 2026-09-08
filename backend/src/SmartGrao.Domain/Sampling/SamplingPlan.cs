using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Geo;
using SmartGrao.Domain.Sampling.Events;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// O plano de amostragem: a malha georreferenciada que o agronomo vai caminhar num talhao, com o
/// registro de como ela foi derivada.
/// <para>
/// E o agregado central do Pilar 2. Referencia o talhao por id, sem navegacao: a fronteira de
/// consistencia e o plano, nao o talhao. Um talhao pode ter varios planos ao longo da safra — o
/// MIP pede amostragem semanal — e cada um e um documento fechado do que foi decidido naquele dia.
/// </para>
/// </summary>
public sealed class SamplingPlan : AggregateRoot<SamplingPlanId>
{
    private readonly List<SamplingPoint> _points = [];

    private SamplingPlan()
    {
    }

    public FieldId FieldId { get; private set; }

    public SamplingMode Mode { get; private set; }

    /// <summary>Espacamento efetivamente usado, em metros — no Monitoramento, o que a busca achou.</summary>
    public double SpacingMeters { get; private set; }

    /// <summary>Bordadura descartada, em metros. Zero no modo Mapeamento.</summary>
    public double EdgeBufferMeters { get; private set; }

    /// <summary>
    /// Pontos que o protocolo pedia, no modo Monitoramento; <c>null</c> no Mapeamento, onde a
    /// pergunta nao tem numero alvo.
    /// <para>
    /// Guardado ao lado da contagem real de proposito. A malha recortada pelo contorno raramente
    /// bate o alvo exato, e esconder a diferenca — devolvendo so o total — apagaria justamente a
    /// informacao de que este talhao ficou acima ou abaixo do que a fonte pede.
    /// </para>
    /// </summary>
    public int? TargetPointCount { get; private set; }

    /// <summary>
    /// Area do talhao no momento em que o plano foi gerado.
    /// <para>
    /// Instantaneo deliberado. O contorno pode ser redesenhado depois — <c>Field.Redraw</c> existe —
    /// e a partir dai os pontos deste plano podem cair fora do talhao. Guardando a area da geracao,
    /// o plano sabe dizer que esta defasado sem precisar carregar o talhao junto.
    /// </para>
    /// </summary>
    public decimal FieldAreaHectares { get; private set; }

    /// <summary>
    /// Verdadeiro quando o protocolo pedia subdivisao do talhao em vez de mais pontos. Nao impediu
    /// a geracao: informa que o resultado esta fora da faixa que a fonte cobre.
    /// </summary>
    public bool SubdivisionRecommended { get; private set; }

    /// <summary>
    /// O contorno do talhao mudou depois que esta malha foi gerada.
    /// <para>
    /// Os pontos continuam gravados como estao — apagar seria pior, porque uma parte deles pode ja
    /// ter sido caminhada. O que muda e que o plano passa a se declarar defasado, e quem for a campo
    /// sabe que ele pode mandar parar fora da area atual.
    /// </para>
    /// </summary>
    public bool IsOutdated { get; private set; }

    public IReadOnlyList<SamplingPoint> Points => _points.AsReadOnly();

    public int PointCount => _points.Count;

    /// <summary>
    /// Verdadeiro quando o talhao nao comportou os pontos que o protocolo pedia, mesmo na malha mais
    /// densa. Acontece em talhao muito pequeno depois de descontada a bordadura.
    /// </summary>
    public bool FallsShortOfTarget => TargetPointCount is int target && PointCount < target;

    /// <summary>
    /// Plano do modo Monitoramento: quantidade de pontos vinda da tabela do MIP-Soja, bordadura
    /// descartada.
    /// </summary>
    public static SamplingPlan ForMonitoring(FieldId fieldId, Boundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        return From(fieldId, SamplingMode.Monitoring, boundary,
            SamplingGridResolver.ForMonitoring(boundary));
    }

    /// <summary>
    /// Plano do modo Mapeamento: espacamento escolhido, bordadura preservada porque o gradiente da
    /// borda para o interior e o que se quer enxergar.
    /// </summary>
    public static SamplingPlan ForMapping(FieldId fieldId, Boundary boundary, SamplingSpacing spacing)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(spacing);

        return From(fieldId, SamplingMode.Mapping, boundary,
            SamplingGridResolver.ForMapping(boundary, spacing));
    }

    /// <summary>
    /// Marca a malha como defasada apos um redesenho do contorno. Idempotente: redesenhar o talhao
    /// tres vezes nao torna o plano mais defasado do que ele ja esta, e nao mexe no
    /// <see cref="AggregateRoot{TId}.UpdatedAt"/> de novo a cada vez.
    /// </summary>
    public void MarkAsOutdated()
    {
        if (IsOutdated) return;

        IsOutdated = true;
        MarkAsUpdated();
    }

    private static SamplingPlan From(
        FieldId fieldId, SamplingMode mode, Boundary boundary, ResolvedGrid grid)
    {
        var plan = new SamplingPlan
        {
            Id = SamplingPlanId.New(),
            FieldId = fieldId,
            Mode = mode,
            SpacingMeters = grid.Spacing.Meters,
            EdgeBufferMeters = grid.EdgeBuffer.Meters,
            TargetPointCount = grid.TargetPointCount,
            FieldAreaHectares = boundary.AreaHectares,
            SubdivisionRecommended = mode == SamplingMode.Monitoring
                && MipSojaSamplingTable.RecommendsSubdivision(boundary.AreaHectares),
        };

        var sequence = 1;
        foreach (var coordinate in grid.Points)
            plan._points.Add(SamplingPoint.Create(plan.Id, sequence++, coordinate));

        plan.RaiseDomainEvent(new SamplingPlanGeneratedEvent(
            plan.Id, fieldId, mode, plan.PointCount));

        return plan;
    }
}
