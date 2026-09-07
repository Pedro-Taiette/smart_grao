using System.Globalization;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Sampling;

/// <summary>
/// A distancia entre pontos vizinhos da malha, em metros. Objeto de valor: nasce validado e nao
/// existe espacamento negativo ou absurdo.
/// <para>
/// Os tres valores nomeados sao os que o estudo de densidade amostral em soja avaliou de fato
/// (48 ha, Julio de Castilhos/RS, safra 2008/09), com as densidades resultantes anotadas em cada um.
/// Existir <see cref="Of"/> nao os torna sugestoes: fora dessa faixa, quem escolhe assume que esta
/// fora do que foi medido.
/// </para>
/// </summary>
public sealed record SamplingSpacing
{
    /// <summary>
    /// Tetos de sanidade, nao recomendacao agronomica — mesma natureza dos limites de area do
    /// talhao. Abaixo de 10 m a malha vira uma caminhada continua.
    /// <para>
    /// O teto e alto de proposito. Ele nao existe para sugerir espacamento: existe para barrar
    /// numero absurdo. Quem o alcanca e o modo Monitoramento em talhao enorme — no limite de
    /// 50.000 ha do dominio, distribuir os 10 pontos da tabela do MIP-Soja pede cerca de 7 km entre
    /// eles. Um teto menor faria o solver bater na parede e devolver centenas de pontos para um alvo
    /// de dez.
    /// </para>
    /// </summary>
    public const double MinimumMeters = 10d;

    public const double MaximumMeters = 10_000d;

    private SamplingSpacing(double meters) => Meters = meters;

    public double Meters { get; }

    /// <summary>50 m — ~3,8 pontos/ha. Mapas mais detalhados, menor indice de variacao, mais caminhada.</summary>
    public static SamplingSpacing Detailed => new(50d);

    /// <summary>71 m — ~2,0 pontos/ha.</summary>
    public static SamplingSpacing Intermediate => new(71d);

    /// <summary>100 m — ~1,0 ponto/ha. Precisao reduzida, ainda viavel; e o padrao sugerido.</summary>
    public static SamplingSpacing Standard => new(100d);

    public static SamplingSpacing Of(double meters)
    {
        if (double.IsNaN(meters) || double.IsInfinity(meters))
            throw new DomainException(SmartGraoErrors.Sampling.SpacingNotFinite);

        if (meters is < MinimumMeters or > MaximumMeters)
        {
            throw new DomainException(SmartGraoErrors.Sampling.SpacingOutOfRange(
                MinimumMeters, MaximumMeters));
        }

        return new SamplingSpacing(meters);
    }

    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Meters:0.#} m");
}
