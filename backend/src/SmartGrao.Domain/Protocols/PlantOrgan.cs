namespace SmartGrao.Domain.Protocols;

/// <summary>
/// Onde olhar. Fechado num enum porque a fase 5 precisa saber que parte da planta a foto mostra —
/// um modelo treinado em folha nao serve para uma foto de raiz, e texto livre tornaria esse
/// roteamento impossivel.
/// </summary>
public enum PlantOrgan
{
    Undefined = 0,

    /// <summary>Cartucho.</summary>
    Whorl,

    Leaf,

    /// <summary>
    /// Folha da espiga ou a imediatamente abaixo (Fe-1). E a folha padrao das doencas foliares:
    /// a severidade nela correlaciona com a da planta inteira.
    /// </summary>
    EarLeaf,

    /// <summary>Base do colmo.</summary>
    StalkBase,

    Stalk,

    Root,

    /// <summary>Espiga.</summary>
    Ear,

    /// <summary>Pendao.</summary>
    Tassel,

    /// <summary>Estilo-estigma (cabelo da espiga).</summary>
    Silk,

    Seedling,

    WholePlant,
}
