namespace SmartGrao.Domain.Sampling;

/// <summary>
/// A tabela de pontos de amostragem por tamanho de talhao do MIP-Soja, transcrita do material de
/// Correa-Ferreira (Embrapa Soja).
/// <para>
/// O que ela tem de particular e nao ser linear na area: a quantidade <b>satura</b> em 10 pontos e,
/// acima de 100 ha, o protocolo troca de estrategia — em vez de mandar amostrar mais, manda
/// subdividir o talhao. Uma regra do tipo "1 ponto a cada N hectares, sem teto" diverge disso ja na
/// faixa dos 30 ha. Ver <c>docs/amostragem.md</c>.
/// </para>
/// </summary>
public static class MipSojaSamplingTable
{
    /// <summary>
    /// Acima disso a fonte nao da um numero de pontos: da a instrucao de dividir a area em talhoes e
    /// repetir o procedimento.
    /// </summary>
    public const decimal SubdivisionThresholdHectares = 100m;

    /// <summary>
    /// Pontos de amostragem que o protocolo pede para a area dada.
    /// <para>
    /// Abaixo de 1 ha a tabela nao chega — a faixa mais baixa e "1 a 9 ha". Talhoes menores herdam os
    /// 6 pontos dessa faixa: e o piso do protocolo, e reduzir por conta propria seria inventar uma
    /// faixa que a fonte nao tem.
    /// </para>
    /// <para>
    /// Acima de 100 ha devolve os mesmos 10 pontos da faixa anterior, mas ai o numero e apenas o que
    /// se consegue fazer sem subdividir; quem chama deve consultar <see cref="RecommendsSubdivision"/>
    /// e avisar.
    /// </para>
    /// </summary>
    public static int TargetPointsFor(decimal areaHectares) => areaHectares switch
    {
        < 10m => 6,
        < 30m => 8,
        _ => 10,
    };

    /// <summary>
    /// Verdadeiro quando o protocolo pede subdivisao do talhao em vez de mais pontos. Nao impede a
    /// geracao do plano: informa que o resultado esta fora da faixa que a fonte cobre.
    /// </summary>
    public static bool RecommendsSubdivision(decimal areaHectares) =>
        areaHectares > SubdivisionThresholdHectares;
}
