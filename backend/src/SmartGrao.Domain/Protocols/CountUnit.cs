namespace SmartGrao.Domain.Protocols;

/// <summary>
/// A unidade em que a contagem de um alvo faz sentido. Conjunto fechado de proposito: e o que
/// permite o protocolo ser generico sem que o numero digitado em campo perca significado — "12" so
/// quer dizer alguma coisa depois de se saber se sao plantas atacadas, insetos por armadilha ou
/// percentual de area foliar lesionada.
/// <para>
/// A unidade carrega tambem o metodo: contagem em 10 m de fileira e armadilha adesiva nao sao o
/// mesmo trabalho de campo que inspecionar plantas ao acaso. Ver <c>docs/fase-2-catalogo-alvos.md</c>.
/// </para>
/// </summary>
public enum CountUnit
{
    Undefined = 0,

    /// <summary>% de plantas com o dano descrito, sobre as plantas avaliadas.</summary>
    AttackedPlantPercentage,

    /// <summary>Plantas atacadas contadas em 10 m de fileira.</summary>
    AttackedPlantsPer10MetreRow,

    /// <summary>Insetos numa planta.</summary>
    InsectsPerPlant,

    /// <summary>Insetos contados sobre dez plantas.</summary>
    InsectsPerTenPlants,

    /// <summary>Capturas por armadilha entre duas trocas.</summary>
    InsectsPerTrap,

    /// <summary>Severidade por escala diagramatica, em % de area foliar lesionada.</summary>
    LesionedLeafAreaPercentage,

    /// <summary>Nota visual de 1 (resistente) a 9 (suscetivel).</summary>
    SeverityScore1To9,

    /// <summary>
    /// Presenca ou ausencia, sem contagem. Existe porque a cigarrinha-do-milho nao tem nivel de
    /// controle estabelecido: e vetor de enfezamento e a presenca ja justifica acao. Sem esta
    /// unidade, o alvo mais importante da safrinha nao caberia no modelo.
    /// </summary>
    Presence,
}

public static class CountUnitRules
{
    public static bool IsPercentage(this CountUnit unit) => unit
        is CountUnit.AttackedPlantPercentage or CountUnit.LesionedLeafAreaPercentage;

    /// <summary>A armadilha nao observa um orgao da planta; todo o resto observa.</summary>
    public static bool RequiresOrgan(this CountUnit unit) => unit is not CountUnit.InsectsPerTrap;

    public static bool AcceptsReferenceLevel(this CountUnit unit) => unit is not CountUnit.Presence;

    /// <summary>Faixa em que um nivel de referencia e sequer plausivel para a unidade.</summary>
    public static (decimal Minimum, decimal Maximum) ReferenceRange(this CountUnit unit) => unit switch
    {
        _ when unit.IsPercentage() => (0m, 100m),
        CountUnit.SeverityScore1To9 => (1m, 9m),
        _ => (0m, decimal.MaxValue),
    };
}
