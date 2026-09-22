using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

// ── Catalogo de alvos ────────────────────────────────────────────────────────

public sealed record CreateMonitoringTargetViewModel(
    string Code, string CommonName, string ScientificName, TargetKind Kind, Crop Crop);

public sealed record ChangeAutomationViewModel(AutomationCapability Automation);

public sealed record MonitoringTargetViewModel(
    Guid Id, string Code, string CommonName, string ScientificName, TargetKind Kind, Crop Crop,
    AutomationCapability Automation)
{
    public static MonitoringTargetViewModel From(MonitoringTarget target) => new(
        target.Id.Value, target.Code, target.CommonName, target.ScientificName,
        target.Kind, target.Crop, target.Automation);
}

// ── Protocolos ───────────────────────────────────────────────────────────────

public sealed record CreateProtocolViewModel(string Code, Crop Crop, string Name);

public sealed record RenameProtocolViewModel(string Name);

public sealed record ReferenceLevelViewModel(decimal Threshold, string Source);

public sealed record AddProtocolItemViewModel(
    Guid TargetId, PlantOrgan? Organ, CountUnit Unit, int PhotosRequested, string Instructions,
    ReferenceLevelViewModel? ReferenceLevel);

public sealed record ProtocolItemViewModel(
    Guid Id, Guid TargetId, string TargetCode, string TargetCommonName,
    AutomationCapability TargetAutomation, PlantOrgan? Organ, CountUnit Unit,
    int PhotosRequested, string Instructions, ReferenceLevelViewModel? ReferenceLevel);

/// <summary>Resumo para listas: nao carrega os alvos.</summary>
public sealed record ProtocolSummaryViewModel(
    Guid Id, string Code, int Version, Crop Crop, string Name, ProtocolStatus Status, int TargetCount)
{
    public static ProtocolSummaryViewModel From(Protocol protocol) => new(
        protocol.Id.Value, protocol.Code, protocol.Version, protocol.Crop, protocol.Name,
        protocol.Status, protocol.Items.Count);
}

public sealed record ProtocolViewModel(
    Guid Id, string Code, int Version, Crop Crop, string Name, ProtocolStatus Status,
    IReadOnlyList<ProtocolItemViewModel> Items)
{
    /// <summary>
    /// Os alvos entram por fora porque o item guarda so o id: a capacidade de automacao vive no
    /// catalogo, e copia-la para dentro do protocolo faria a tela mostrar o estado do dia em que a
    /// versao foi publicada, nao o de hoje.
    /// </summary>
    public static ProtocolViewModel From(Protocol protocol, IReadOnlyDictionary<MonitoringTargetId, MonitoringTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(protocol);
        ArgumentNullException.ThrowIfNull(targets);

        var items = protocol.Items
            .Select(item =>
            {
                var target = targets[item.TargetId];
                return new ProtocolItemViewModel(
                    item.Id.Value, item.TargetId.Value, target.Code, target.CommonName,
                    target.Automation, item.Organ, item.Unit, item.PhotosRequested, item.Instructions,
                    item.ReferenceLevel is null
                        ? null
                        : new ReferenceLevelViewModel(item.ReferenceLevel.Threshold, item.ReferenceLevel.Source));
            })
            .OrderBy(item => item.TargetCommonName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new ProtocolViewModel(
            protocol.Id.Value, protocol.Code, protocol.Version, protocol.Crop, protocol.Name,
            protocol.Status, items);
    }
}
