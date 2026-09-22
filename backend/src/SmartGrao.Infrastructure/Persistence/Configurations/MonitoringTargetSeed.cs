using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Infrastructure.Persistence.Configurations;

/// <summary>
/// O catalogo de alvos de milho que acompanha o produto. E dado de referencia, nao conteudo de uma
/// propriedade: o mesmo alvo vale para qualquer lavoura de milho do pais, entao ele vem na migration
/// em vez de ser recadastrado fazenda a fazenda.
/// <para>
/// Todos entram como <see cref="AutomationCapability.ManualRecord"/>. A cobertura ampla vem do
/// tamanho do catalogo; nenhum destes alvos foi validado por um modelo, e o enum e o que impede a
/// lista de ser lida como promessa de diagnostico automatico.
/// </para>
/// <para>
/// Ids sinteticos e estaveis de proposito — a linha precisa sobreviver a uma reaplicacao da seed, e
/// um Guid sorteado em tempo de build criaria um alvo novo a cada migration. Ver
/// <c>docs/fase-2-catalogo-alvos.md</c> para as fontes e para o que nao foi confirmado.
/// </para>
/// </summary>
internal static class MonitoringTargetSeed
{
    /// <summary>Data fixa: a seed precisa gerar sempre o mesmo INSERT.</summary>
    private static readonly DateTimeOffset SeededAt = new(2026, 9, 22, 0, 0, 0, TimeSpan.Zero);

    internal static IReadOnlyList<object> Rows { get; } =
    [
        // ── Pragas ────────────────────────────────────────────────────────────
        Row(1, "spodoptera_frugiperda", "Lagarta-do-cartucho", "Spodoptera frugiperda", TargetKind.Pest),
        Row(2, "dalbulus_maidis", "Cigarrinha-do-milho", "Dalbulus maidis", TargetKind.Pest),
        Row(3, "diceraeus_melacanthus", "Percevejo-barriga-verde", "Diceraeus melacanthus", TargetKind.Pest),
        Row(4, "rhopalosiphum_maidis", "Pulgão-do-milho", "Rhopalosiphum maidis", TargetKind.Pest),
        Row(5, "elasmopalpus_lignosellus", "Lagarta-elasmo", "Elasmopalpus lignosellus", TargetKind.Pest),
        Row(6, "agrotis_ipsilon", "Lagarta-rosca", "Agrotis ipsilon", TargetKind.Pest),
        Row(7, "diatraea_saccharalis", "Broca-da-cana", "Diatraea saccharalis", TargetKind.Pest),
        Row(8, "helicoverpa_zea", "Lagarta-da-espiga", "Helicoverpa zea", TargetKind.Pest),
        Row(9, "diabrotica_speciosa", "Larva-alfinete", "Diabrotica speciosa", TargetKind.Pest),
        Row(10, "coros", "Corós", "Diloboderus abderus e Phyllophaga spp.", TargetKind.Pest),

        // ── Doencas foliares ──────────────────────────────────────────────────
        Row(11, "pantoea_ananatis", "Mancha-branca", "Pantoea ananatis", TargetKind.FoliarDisease),
        Row(12, "cercospora_zeina", "Cercosporiose", "Cercospora zeina", TargetKind.FoliarDisease),
        Row(13, "exserohilum_turcicum", "Helmintosporiose", "Exserohilum turcicum", TargetKind.FoliarDisease),
        Row(14, "colletotrichum_graminicola", "Antracnose foliar", "Colletotrichum graminicola", TargetKind.FoliarDisease),
        Row(15, "bipolaris_maydis", "Mancha-de-bipolaris", "Bipolaris maydis", TargetKind.FoliarDisease),
        Row(16, "puccinia_polysora", "Ferrugem-polissora", "Puccinia polysora", TargetKind.FoliarDisease),
        Row(17, "puccinia_sorghi", "Ferrugem-comum", "Puccinia sorghi", TargetKind.FoliarDisease),
        Row(18, "physopella_zeae", "Ferrugem-tropical", "Physopella zeae", TargetKind.FoliarDisease),
    ];

    private static object Row(int index, string code, string commonName, string scientificName, TargetKind kind) => new
    {
        Id = new MonitoringTargetId(new Guid($"a1000000-0000-4000-8000-{index:D12}")),
        Code = code,
        CommonName = commonName,
        ScientificName = scientificName,
        Kind = kind,
        Crop = Crop.Corn,
        Automation = AutomationCapability.ManualRecord,
        CreatedAt = SeededAt,
    };
}
