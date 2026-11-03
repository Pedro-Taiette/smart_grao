using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Cultivations;

public enum HybridReaction
{
    Unknown = 0,
    Resistant,
    ModeratelyResistant,
    ModeratelySusceptible,
    Susceptible,
}

public enum CropSequence
{
    Unknown = 0,
    CornAfterCorn,
    Rotation,
}

public enum TillageSystem
{
    Unknown = 0,
    NoTill,
    Conventional,
}

/// <summary>
/// Niveis ordinais, nao pesos: a literatura ordena esses fatores, mas nao publica multiplicador.
/// Ver <c>docs/pesquisa-cercosporiose.md</c>.
/// </summary>
public sealed record CercosporaRiskFactors
{
    public static CercosporaRiskFactors Unknown { get; } = new();

    private CercosporaRiskFactors() { }

    public HybridReaction HybridReaction { get; private init; }

    public CropSequence CropSequence { get; private init; }

    public TillageSystem Tillage { get; private init; }

    public bool IsKnown => HybridReaction != HybridReaction.Unknown
        || CropSequence != CropSequence.Unknown || Tillage != TillageSystem.Unknown;

    public static CercosporaRiskFactors Create(HybridReaction hybridReaction, CropSequence cropSequence, TillageSystem tillage)
    {
        if (!Enum.IsDefined(hybridReaction) || !Enum.IsDefined(cropSequence) || !Enum.IsDefined(tillage))
            throw new DomainException(SmartGraoErrors.Cultivation.UnknownRiskFactor);
        return new CercosporaRiskFactors { HybridReaction = hybridReaction, CropSequence = cropSequence, Tillage = tillage };
    }
}
