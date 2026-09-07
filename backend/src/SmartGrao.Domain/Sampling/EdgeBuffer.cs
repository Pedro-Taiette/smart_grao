using System.Globalization;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// A faixa de bordadura descartada antes de gerar a malha, em metros.
/// <para>
/// Existe como objeto de valor com <see cref="DefaultFor"/> — e nao como parametro que a API exige —
/// de proposito. Se a distancia viesse de fora, uma regra agronomica passaria a ser decidida pelo
/// frontend ou por um arquivo de configuracao; no segundo caso ela ainda sairia do codigo
/// versionado, disfarcada de ajuste de infraestrutura. O default mora aqui, e um valor explicito de
/// quem chama e uma decisao consciente, nao o preenchimento de um campo obrigatorio.
/// </para>
/// </summary>
public sealed record EdgeBuffer
{
    private EdgeBuffer(double meters) => Meters = meters;

    public double Meters { get; }

    public bool DiscardsAnything => Meters > 0d;

    /// <summary>Nenhum descarte. E o default do modo Mapeamento.</summary>
    public static EdgeBuffer None => new(0d);

    public static EdgeBuffer Of(double meters)
    {
        if (double.IsNaN(meters) || double.IsInfinity(meters))
            throw new DomainException(SmartGraoErrors.Sampling.EdgeBufferNotFinite);

        if (meters < 0d)
            throw new DomainException(SmartGraoErrors.Sampling.EdgeBufferNegative);

        return new EdgeBuffer(meters);
    }

    /// <summary>
    /// A bordadura que cada modo descarta por padrao.
    /// <para>
    /// <b>Mapeamento nao descarta nada.</b> A pergunta ali e onde aplicar, e a fonte da Embrapa
    /// mostra a populacao de percevejos caindo de 67 para 0 por metro da borda para o interior.
    /// Esse gradiente <i>e</i> o sinal — apagar a borda apagaria justamente a zona que justifica a
    /// aplicacao em faixa.
    /// </para>
    /// <para>
    /// <b>Monitoramento descarta meio espacamento.</b> Ali a pergunta e a media do talhao, e um
    /// ponto na borda a contamina em uma ordem de grandeza. A distancia em metros nao esta
    /// confirmada em fonte primaria, entao ela e <i>derivada</i> em vez de escolhida: um ponto de
    /// uma grade regular representa uma celula de lado igual ao espacamento, e um ponto colado na
    /// divisa teria metade da sua celula fora do talhao. Recuando meio espacamento, toda celula
    /// retida cabe inteira dentro da area.
    /// </para>
    /// </summary>
    public static EdgeBuffer DefaultFor(SamplingMode mode, SamplingSpacing spacing)
    {
        ArgumentNullException.ThrowIfNull(spacing);

        return mode switch
        {
            SamplingMode.Mapping => None,
            SamplingMode.Monitoring => new EdgeBuffer(spacing.Meters / 2d),
            _ => throw new DomainException(SmartGraoErrors.Sampling.UnknownMode),
        };
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Meters:0.#} m");
}
