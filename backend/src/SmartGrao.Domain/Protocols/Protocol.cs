using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Protocols;

public readonly record struct ProtocolId(Guid Value) : IStronglyTypedId
{
    public static ProtocolId New() => new(Guid.CreateVersion7());
}

public readonly record struct ProtocolItemId(Guid Value) : IStronglyTypedId
{
    public static ProtocolItemId New() => new(Guid.CreateVersion7());
}

public enum ProtocolStatus
{
    /// <summary>Em edicao. Nenhuma vistoria pode se apoiar nele.</summary>
    Draft = 0,

    /// <summary>Em uso e imutavel.</summary>
    Published,

    /// <summary>Fora de uso para vistorias novas; segue legivel pelo historico que o referencia.</summary>
    Retired,
}

/// <summary>
/// Nivel de referencia de um alvo, com a fonte junto. Os dois andam sempre no mesmo objeto porque um
/// numero agronomico sem procedencia nao e melhor que numero nenhum — e a pesquisa da fase 2 achou
/// valor publicado para so tres dos dezoito alvos.
/// </summary>
public sealed record ReferenceLevel
{
    public const int MaximumSourceLength = 300;

    private ReferenceLevel() { }

    public decimal Threshold { get; private init; }

    public string Source { get; private init; } = string.Empty;

    public static ReferenceLevel Create(decimal threshold, string source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Trim().Length > MaximumSourceLength)
            throw new DomainException(SmartGraoErrors.Protocol.ReferenceLevelWithoutSource);

        return new ReferenceLevel { Threshold = threshold, Source = source.Trim() };
    }
}

/// <summary>Um alvo dentro de um protocolo: o que olhar, onde, em que unidade e o que fotografar.</summary>
public sealed class ProtocolItem : Entity<ProtocolItemId>
{
    public const int MaximumInstructionsLength = 2000;
    public const int MaximumPhotosRequested = 5;

    private ProtocolItem() { }

    public ProtocolId ProtocolId { get; private set; }

    public MonitoringTargetId TargetId { get; private set; }

    /// <summary>Nulo apenas quando a unidade nao observa a planta — o caso da armadilha.</summary>
    public PlantOrgan? Organ { get; private set; }

    public CountUnit Unit { get; private set; }

    /// <summary>
    /// Opcional de proposito. A maioria dos alvos e decidida "em funcao do nivel de dano", sem
    /// tabela publicada, e as doencas foliares nao tem nivel de dano economico de uso corrente.
    /// Exigir um numero aqui obrigaria alguem a inventa-lo.
    /// </summary>
    public ReferenceLevel? ReferenceLevel { get; private set; }

    public int PhotosRequested { get; private set; }

    public string Instructions { get; private set; } = string.Empty;

    internal static ProtocolItem Create(
        ProtocolId protocolId, MonitoringTargetId targetId, PlantOrgan? organ, CountUnit unit,
        int photosRequested, string instructions, ReferenceLevel? referenceLevel)
    {
        if (!Enum.IsDefined(unit) || unit == CountUnit.Undefined)
            throw new DomainException(SmartGraoErrors.Protocol.UnknownUnit);

        if (unit.RequiresOrgan())
        {
            if (organ is not { } value || !Enum.IsDefined(value) || value == PlantOrgan.Undefined)
                throw new DomainException(SmartGraoErrors.Protocol.OrganRequired);
        }
        else if (organ is not null)
        {
            throw new DomainException(SmartGraoErrors.Protocol.OrganNotApplicable);
        }

        if (referenceLevel is not null)
        {
            if (!unit.AcceptsReferenceLevel())
                throw new DomainException(SmartGraoErrors.Protocol.ReferenceLevelNotApplicable);

            var (minimum, maximum) = unit.ReferenceRange();
            if (referenceLevel.Threshold <= 0m
                || referenceLevel.Threshold < minimum || referenceLevel.Threshold > maximum)
                throw new DomainException(SmartGraoErrors.Protocol.ReferenceLevelOutOfRange);
        }

        if (photosRequested < 0 || photosRequested > MaximumPhotosRequested)
            throw new DomainException(SmartGraoErrors.Protocol.TooManyPhotos(MaximumPhotosRequested));

        if (string.IsNullOrWhiteSpace(instructions)
            || instructions.Trim().Length > MaximumInstructionsLength)
            throw new DomainException(SmartGraoErrors.Protocol.InvalidInstructions);

        return new ProtocolItem
        {
            Id = ProtocolItemId.New(),
            ProtocolId = protocolId,
            TargetId = targetId,
            Organ = organ,
            Unit = unit,
            ReferenceLevel = referenceLevel,
            PhotosRequested = photosRequested,
            Instructions = instructions.Trim(),
        };
    }

    internal ProtocolItem CopyTo(ProtocolId protocolId) => new()
    {
        Id = ProtocolItemId.New(),
        ProtocolId = protocolId,
        TargetId = TargetId,
        Organ = Organ,
        Unit = Unit,
        // Instancia propria, e nao a mesma referencia: o nivel e um objeto <i>owned</i>, cuja chave
        // e o item que o possui. Compartilha-lo entre duas versoes faria a persistencia entender
        // que o nivel da v1 mudou de dono ao gravar a v2.
        ReferenceLevel = ReferenceLevel is null
            ? null
            : Protocols.ReferenceLevel.Create(ReferenceLevel.Threshold, ReferenceLevel.Source),
        PhotosRequested = PhotosRequested,
        Instructions = Instructions,
    };
}

/// <summary>
/// O que coletar numa vistoria, para uma cultura. Versionado e imutavel depois de publicado: a
/// vistoria da fase 3 aponta para uma versao exata, entao editar o protocolo nunca reescreve o que
/// foi pedido numa visita ja realizada.
/// <para>
/// O protocolo declara a sua cultura e so aceita alvos dela. E isso que impede uma vistoria de milho
/// de herdar o MIP-Soja por omissao — nao existe caminho no modelo em que os alvos de soja caiam
/// num protocolo de milho.
/// </para>
/// </summary>
public sealed class Protocol : AggregateRoot<ProtocolId>
{
    public const int MaximumCodeLength = 60;
    public const int MaximumNameLength = 120;

    private readonly List<ProtocolItem> _items = [];

    private Protocol() { }

    /// <summary>Identidade da familia de versoes (<c>milho_padrao</c>), estavel entre elas.</summary>
    public string Code { get; private set; } = string.Empty;

    public int Version { get; private set; }

    public Crop Crop { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public ProtocolStatus Status { get; private set; }

    public IReadOnlyList<ProtocolItem> Items => _items.AsReadOnly();

    public static Protocol CreateDraft(string code, Crop crop, string name)
    {
        if (!Enum.IsDefined(crop) || crop == Crop.Undefined)
            throw new DomainException(SmartGraoErrors.Field.UnknownCrop);

        return new Protocol
        {
            Id = ProtocolId.New(),
            Code = ValidateCode(code),
            Version = 1,
            Crop = crop,
            Name = ValidateName(name),
            Status = ProtocolStatus.Draft,
        };
    }

    /// <summary>
    /// Abre a proxima versao a partir desta, copiando os alvos. Parte de uma versao publicada: uma
    /// minuta ainda se edita no lugar, e ramificar dela produziria duas minutas concorrentes da
    /// mesma familia sem que nenhuma tenha sido usada.
    /// </summary>
    public Protocol CreateNextVersion()
    {
        if (Status != ProtocolStatus.Published)
            throw new DomainException(SmartGraoErrors.Protocol.NotPublished);

        var next = new Protocol
        {
            Id = ProtocolId.New(),
            Code = Code,
            Version = Version + 1,
            Crop = Crop,
            Name = Name,
            Status = ProtocolStatus.Draft,
        };

        next._items.AddRange(_items.Select(item => item.CopyTo(next.Id)));

        return next;
    }

    public void Rename(string name)
    {
        EnsureDraft();
        Name = ValidateName(name);
        MarkAsUpdated();
    }

    public ProtocolItem AddItem(
        MonitoringTarget target, PlantOrgan? organ, CountUnit unit, int photosRequested,
        string instructions, ReferenceLevel? referenceLevel = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        EnsureDraft();

        if (target.Crop != Crop)
            throw new DomainException(SmartGraoErrors.Protocol.TargetFromAnotherCrop);
        if (_items.Any(item => item.TargetId == target.Id && item.Organ == organ))
            throw new DomainException(SmartGraoErrors.Protocol.DuplicateItem);

        var created = ProtocolItem.Create(
            Id, target.Id, organ, unit, photosRequested, instructions, referenceLevel);

        _items.Add(created);
        MarkAsUpdated();

        return created;
    }

    public void RemoveItem(ProtocolItemId itemId)
    {
        EnsureDraft();

        var item = _items.FirstOrDefault(x => x.Id == itemId)
            ?? throw new DomainException(SmartGraoErrors.Protocol.ItemNotFound);

        _items.Remove(item);
        MarkAsUpdated();
    }

    public void Publish()
    {
        if (Status == ProtocolStatus.Published) return;
        EnsureDraft();

        if (_items.Count == 0)
            throw new DomainException(SmartGraoErrors.Protocol.EmptyProtocol);

        Status = ProtocolStatus.Published;
        MarkAsUpdated();
    }

    public void Retire()
    {
        if (Status == ProtocolStatus.Retired) return;
        if (Status != ProtocolStatus.Published)
            throw new DomainException(SmartGraoErrors.Protocol.NotPublished);

        Status = ProtocolStatus.Retired;
        MarkAsUpdated();
    }

    private void EnsureDraft()
    {
        if (Status != ProtocolStatus.Draft)
            throw new DomainException(SmartGraoErrors.Protocol.NotDraft);
    }

    private static string ValidateCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(SmartGraoErrors.Protocol.InvalidCode);

        var trimmed = code.Trim();

        if (trimmed.Length > MaximumCodeLength
            || trimmed.StartsWith('_') || trimmed.EndsWith('_')
            || !trimmed.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_'))
            throw new DomainException(SmartGraoErrors.Protocol.InvalidCode);

        return trimmed;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Protocol.InvalidName);

        return name.Trim();
    }
}
