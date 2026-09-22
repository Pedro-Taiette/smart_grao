using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Protocols;

public readonly record struct MonitoringTargetId(Guid Value) : IStronglyTypedId
{
    public static MonitoringTargetId New() => new(Guid.CreateVersion7());
}

/// <summary>O que a equipe observa. Ver <c>docs/fase-2-catalogo-alvos.md</c>.</summary>
public enum TargetKind
{
    Undefined = 0,
    Pest,
    FoliarDisease,
}

/// <summary>
/// Ate onde a automacao chegou <b>neste alvo</b>, e nao no produto como um todo: o catalogo e amplo
/// justamente porque a maior parte dele nunca passou por um modelo.
/// <para>
/// <see cref="ManualRecord"/> e o zero de proposito. Um alvo cadastrado sem que ninguem tenha
/// pensado nisso nasce sem promessa de diagnostico automatico, que e o lado seguro do engano.
/// </para>
/// </summary>
public enum AutomationCapability
{
    /// <summary>Disponivel para registro e acompanhamento; nenhuma analise automatica.</summary>
    ManualRecord = 0,

    /// <summary>Modelo em avaliacao, sempre com revisao humana antes de virar resultado.</summary>
    UnderValidation,

    /// <summary>Modelo liberado para um uso definido, mantendo rastreabilidade.</summary>
    AutomationEnabled,
}

/// <summary>
/// Uma praga ou doenca foliar do catalogo. E dado de referencia do produto, compartilhado por todas
/// as propriedades — nao existe alvo "de uma fazenda".
/// </summary>
public sealed class MonitoringTarget : AggregateRoot<MonitoringTargetId>
{
    public const int MaximumCodeLength = 60;
    public const int MaximumNameLength = 120;

    private MonitoringTarget() { }

    /// <summary>
    /// Chave estavel e legivel (<c>spodoptera_frugiperda</c>). O nome comum varia por regiao e o
    /// Guid nao diz nada; e este codigo que vai rotular imagem e resultado de modelo na fase 5,
    /// entao ele precisa sobreviver a renomeacoes.
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    public string CommonName { get; private set; } = string.Empty;

    public string ScientificName { get; private set; } = string.Empty;

    public TargetKind Kind { get; private set; }

    public Crop Crop { get; private set; }

    public AutomationCapability Automation { get; private set; }

    public static MonitoringTarget Create(
        string code, string commonName, string scientificName, TargetKind kind, Crop crop)
    {
        if (!Enum.IsDefined(kind) || kind == TargetKind.Undefined)
            throw new DomainException(SmartGraoErrors.Protocol.UnknownTargetKind);
        if (!Enum.IsDefined(crop) || crop == Crop.Undefined)
            throw new DomainException(SmartGraoErrors.Field.UnknownCrop);

        return new MonitoringTarget
        {
            Id = MonitoringTargetId.New(),
            Code = ValidateCode(code),
            CommonName = ValidateName(commonName),
            ScientificName = ValidateName(scientificName),
            Kind = kind,
            Crop = crop,
        };
    }

    /// <summary>
    /// Move o alvo na escada da automacao.
    /// <para>
    /// Subir de <see cref="AutomationCapability.ManualRecord"/> direto para
    /// <see cref="AutomationCapability.AutomationEnabled"/> e recusado: liberar um modelo sem passar
    /// pela revisao humana e exatamente o que transformaria o catalogo amplo numa promessa de
    /// diagnostico que ninguem verificou. Descer e sempre permitido — um modelo que regrediu em
    /// campo precisa voltar para revisao no mesmo dia, sem passo intermediario.
    /// </para>
    /// </summary>
    public void ChangeAutomation(AutomationCapability capability)
    {
        if (!Enum.IsDefined(capability))
            throw new DomainException(SmartGraoErrors.Protocol.UnknownAutomation);
        if (capability == Automation) return;
        if (Automation == AutomationCapability.ManualRecord
            && capability == AutomationCapability.AutomationEnabled)
            throw new DomainException(SmartGraoErrors.Protocol.AutomationSkipsValidation);

        Automation = capability;
        MarkAsUpdated();
    }

    public void Rename(string commonName, string scientificName)
    {
        CommonName = ValidateName(commonName);
        ScientificName = ValidateName(scientificName);
        MarkAsUpdated();
    }

    private static string ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(SmartGraoErrors.Protocol.InvalidTargetCode);

        var trimmed = code.Trim();

        if (trimmed.Length > MaximumCodeLength
            || trimmed.StartsWith('_') || trimmed.EndsWith('_')
            || !trimmed.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'))
            throw new DomainException(SmartGraoErrors.Protocol.InvalidTargetCode);

        return trimmed;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Protocol.InvalidTargetName);

        return name.Trim();
    }
}
