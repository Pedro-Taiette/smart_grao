using System.Globalization;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Cultivations;

/// <summary>Um estádio da escala, com o código e o que ele descreve.</summary>
public sealed record GrowthStage(string Code, string Description);

/// <summary>
/// As escalas fenologicas por cultura, transcritas da literatura: milho por Ritchie, Hanway &amp;
/// Benson e soja por Fehr &amp; Caviness. Tabela em codigo, como
/// <see cref="Sampling.MipSojaSamplingTable"/> — sao escalas cientificas fixas, nao conteudo que
/// alguem edita.
/// <para>
/// A fase 1 gravava o estadio como texto livre e deixou a validacao por cultura para a fase 2. E o
/// que esta aqui: num cultivo de milho, "V6" e um estadio e "seis folhas" nao e, porque quem for
/// comparar dois ciclos ou alimentar um modelo precisa do mesmo codigo nos dois.
/// </para>
/// <para>
/// Cultura sem escala transcrita continua aceitando texto livre. Inventar uma escala para o algodao
/// so para fechar a simetria seria pior do que admitir que ela nao esta aqui.
/// </para>
/// </summary>
public static class GrowthStageCatalog
{
    /// <summary>
    /// Teto dos estadios vegetativos. O numero de folhas varia com o hibrido e a cultivar, e a
    /// escala nao tem fim definido — 20 cobre com folga o que se observa em campo sem transformar a
    /// lista num rolo interminavel na tela.
    /// </summary>
    public const int MaximumVegetativeStage = 20;

    private static readonly IReadOnlyList<GrowthStage> CornStages = BuildCorn();
    private static readonly IReadOnlyList<GrowthStage> SoybeanStages = BuildSoybean();

    /// <summary>A escala da cultura, ou uma lista vazia quando nao houver escala transcrita.</summary>
    public static IReadOnlyList<GrowthStage> For(Crop crop) => crop switch
    {
        Crop.Corn => CornStages,
        Crop.Soybean => SoybeanStages,
        _ => [],
    };

    public static bool HasScale(Crop crop) => For(crop).Count > 0;

    /// <summary>
    /// Resolve o que foi digitado para o codigo canonico da escala — "v6" e "V6" sao o mesmo
    /// estadio, e gravar os dois separaria em duas coisas o que e uma so.
    /// <para>
    /// Devolve <c>false</c> apenas quando a cultura <b>tem</b> escala e o codigo nao esta nela. Sem
    /// escala, aceita o texto dentro do limite de tamanho.
    /// </para>
    /// </summary>
    public static bool TryResolve(Crop crop, string stage, out string code)
    {
        var typed = stage?.Trim() ?? string.Empty;
        code = typed;

        var scale = For(crop);
        if (scale.Count == 0) return typed.Length is > 0 and <= 32;

        var match = scale.FirstOrDefault(
            candidate => string.Equals(candidate.Code, typed, StringComparison.OrdinalIgnoreCase));

        if (match is null) return false;

        code = match.Code;
        return true;
    }

    private static IReadOnlyList<GrowthStage> BuildCorn()
    {
        List<GrowthStage> stages = [new("VE", "Emergência")];
        stages.AddRange(Vegetative("folha totalmente expandida", "folhas totalmente expandidas"));
        stages.Add(new("VT", "Pendoamento"));
        stages.AddRange([
            new("R1", "Florescimento (embonecamento)"),
            new("R2", "Grãos leitosos"),
            new("R3", "Grãos pastosos"),
            new("R4", "Grãos farináceos"),
            new("R5", "Grãos farináceos-duros"),
            new("R6", "Maturidade fisiológica"),
        ]);
        return stages;
    }

    private static IReadOnlyList<GrowthStage> BuildSoybean()
    {
        List<GrowthStage> stages = [new("VE", "Emergência"), new("VC", "Cotilédone")];
        stages.AddRange(Vegetative("folha trifoliolada expandida", "folhas trifolioladas expandidas"));
        stages.AddRange([
            new("R1", "Início do florescimento"),
            new("R2", "Pleno florescimento"),
            new("R3", "Início da formação das vagens"),
            new("R4", "Vagens completamente desenvolvidas"),
            new("R5", "Início do enchimento dos grãos"),
            new("R6", "Grãos cheios"),
            new("R7", "Início da maturação"),
            new("R8", "Maturação plena"),
        ]);
        return stages;
    }

    private static IEnumerable<GrowthStage> Vegetative(string singular, string plural) =>
        Enumerable.Range(1, MaximumVegetativeStage).Select(number => new GrowthStage(
            string.Create(CultureInfo.InvariantCulture, $"V{number}"),
            string.Create(CultureInfo.InvariantCulture, $"{number} {(number == 1 ? singular : plural)}")));
}
