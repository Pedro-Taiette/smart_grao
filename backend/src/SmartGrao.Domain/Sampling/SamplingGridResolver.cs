using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Geo;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// Resolve a malha de um talhao: decide o espacamento, a bordadura e os pontos, para cada modo.
/// <para>
/// E aqui que as duas derivacoes de densidade se encaixam no mesmo
/// <see cref="SamplingGridGenerator"/> por lados opostos. O Mapeamento entra com o espacamento
/// pronto. O Monitoramento entra com uma <i>contagem alvo</i> e precisa descobrir que espacamento a
/// produz — o lado dificil, e o motivo de os dois modos terem sido feitos juntos: uma assinatura
/// desenhada so para o Mapeamento nao acomodaria o outro caso.
/// </para>
/// </summary>
public static class SamplingGridResolver
{
    /// <summary>
    /// Iteracoes da bisseção. Bastam ~17 para reduzir a faixa de 10 m a 10 km a menos de um decimetro
    /// de espacamento; 40 e folga com custo desprezivel.
    /// </summary>
    private const int MaximumIterations = 40;

    /// <summary>
    /// Malha do modo Mapeamento: espacamento escolhido, sem descarte de bordadura.
    /// </summary>
    public static ResolvedGrid ForMapping(Boundary boundary, SamplingSpacing spacing)
    {
        ArgumentNullException.ThrowIfNull(boundary);
        ArgumentNullException.ThrowIfNull(spacing);

        var edgeBuffer = EdgeBuffer.DefaultFor(SamplingMode.Mapping, spacing);
        var points = SamplingGridGenerator.Generate(boundary, spacing);

        if (points.Count == 0)
            throw new DomainException(SmartGraoErrors.Sampling.NoPointsFitTheField);

        return new ResolvedGrid(boundary, spacing, edgeBuffer, TargetPointCount: null, points);
    }

    /// <summary>
    /// Malha do modo Monitoramento: contagem alvo vinda da tabela do MIP-Soja, espacamento
    /// descoberto por busca, bordadura de meio espacamento descartada.
    /// <para>
    /// A grade recortada pelo contorno nao produz a contagem exata do alvo — ela cai onde cai. A
    /// busca e pelo <b>maior espacamento cuja malha ainda entrega pelo menos o alvo</b>, e o
    /// excedente e aceito. Descartar pontos ate bater o numero exigiria um criterio que nenhuma
    /// fonte respalda e que introduziria vies espacial; o protocolo, por sua vez, estabelece um
    /// minimo de pontos, nao um teto.
    /// </para>
    /// </summary>
    public static ResolvedGrid ForMonitoring(Boundary boundary)
    {
        ArgumentNullException.ThrowIfNull(boundary);

        var target = MipSojaSamplingTable.TargetPointsFor(boundary.AreaHectares);

        // A bordadura depende do espacamento, que depende da contagem, que depende da bordadura. O
        // no se desfaz fixando a bordadura pelo espacamento-semente — o que a area bruta sugere — e
        // nao reavaliando dentro da busca. O alternativa seria recuar o contorno a cada iteracao,
        // que alem de caro faria a area de amostragem mudar debaixo da propria busca.
        var seed = SeedSpacing(boundary.AreaHectares, target);
        var edgeBuffer = EdgeBuffer.DefaultFor(SamplingMode.Monitoring, seed);

        var samplingArea = boundary.Shrink(edgeBuffer.Meters)
            ?? throw new DomainException(SmartGraoErrors.Sampling.FieldTooNarrowForEdgeBuffer);

        var spacing = LargestSpacingReaching(samplingArea, target);
        var points = SamplingGridGenerator.Generate(samplingArea, spacing);

        if (points.Count == 0)
            throw new DomainException(SmartGraoErrors.Sampling.NoPointsFitTheField);

        return new ResolvedGrid(samplingArea, spacing, edgeBuffer, target, points);
    }

    /// <summary>
    /// Chute inicial do espacamento: o lado da celula que dividiria a area bruta em exatamente
    /// <paramref name="target"/> celulas. E o valor exato para um talhao quadrado sem recorte, e um
    /// ponto de partida honesto para qualquer outro.
    /// </summary>
    private static SamplingSpacing SeedSpacing(decimal areaHectares, int target)
    {
        var areaSquareMeters = (double)areaHectares * Wgs84Geodesy.SquareMetersPerHectare;
        var side = Math.Sqrt(areaSquareMeters / target);

        return SamplingSpacing.Of(Math.Clamp(
            side, SamplingSpacing.MinimumMeters, SamplingSpacing.MaximumMeters));
    }

    /// <summary>
    /// Maior espacamento cuja malha ainda entrega pelo menos <paramref name="target"/> pontos.
    /// <para>
    /// A contagem nao e estritamente monotonica no espacamento: o alinhamento da grade faz a curva
    /// oscilar de um ponto ou dois. Por isso a busca nao confia na monotonicidade para estar certa —
    /// ela mantem uma <b>invariante</b>. O limite inferior sempre satisfaz o alvo, e e ele que e
    /// devolvido. O resultado pode nao ser o maximo verdadeiro em uma geometria adversa, mas nunca
    /// deixa de cumprir a garantia de entregar o alvo.
    /// </para>
    /// <para>
    /// Quando nem o espacamento minimo alcanca o alvo — talhao pequeno demais para caber os pontos
    /// que o protocolo pede —, devolve o minimo: a malha mais densa possivel. O plano registra alvo
    /// e contagem real lado a lado, entao a diferenca fica visivel em vez de escondida.
    /// </para>
    /// </summary>
    private static SamplingSpacing LargestSpacingReaching(Boundary samplingArea, int target)
    {
        var low = SamplingSpacing.MinimumMeters;
        var high = SamplingSpacing.MaximumMeters;

        // Nem a malha mais densa permitida alcanca o alvo: talhao pequeno demais para os pontos que
        // o protocolo pede. Devolve o minimo, que e o melhor que existe.
        if (CountAt(samplingArea, low) < target)
            return SamplingSpacing.Of(low);

        // O outro extremo. Nao deveria acontecer no dominio — no maior talhao possivel, 10 km entre
        // pontos rende menos de dez —, mas se acontecesse a bisseção abaixo nao teria por onde
        // comecar.
        if (CountAt(samplingArea, high) >= target)
            return SamplingSpacing.Of(high);

        for (var i = 0; i < MaximumIterations; i++)
        {
            var middle = (low + high) / 2d;

            if (CountAt(samplingArea, middle) >= target)
                low = middle;
            else
                high = middle;
        }

        return SamplingSpacing.Of(low);
    }

    /// <summary>
    /// Pontos que a malha entregaria naquele espacamento, saturando quando a grade seria densa
    /// demais para ser construida.
    /// <para>
    /// A saturacao nao e atalho de desempenho: sem ela a busca quebra. Ela sonda a faixa inteira de
    /// espacamentos permitidos, e no extremo denso um talhao grande passa de milhoes de candidatos —
    /// o gerador recusaria com <c>grid_too_dense</c> e derrubaria a resolucao de um talhao
    /// perfeitamente valido. Como a comparacao aqui e so contra um alvo de poucas dezenas, "densa
    /// demais para construir" e com folga "acima do alvo".
    /// </para>
    /// </summary>
    private static int CountAt(Boundary samplingArea, double spacingMeters)
    {
        var spacing = SamplingSpacing.Of(spacingMeters);

        return SamplingGridGenerator.CandidateCount(samplingArea, spacing) > SamplingGridGenerator.MaximumPoints
            ? int.MaxValue
            : SamplingGridGenerator.Generate(samplingArea, spacing).Count;
    }
}

/// <summary>
/// O que a resolucao produziu: a area efetivamente amostrada — ja recuada pela bordadura —, o
/// espacamento resolvido, a bordadura aplicada, a contagem alvo quando ela existe e os pontos.
/// </summary>
public sealed record ResolvedGrid(
    Boundary SamplingArea,
    SamplingSpacing Spacing,
    EdgeBuffer EdgeBuffer,
    int? TargetPointCount,
    IReadOnlyList<GeoCoordinate> Points);
