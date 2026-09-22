using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

/// <summary>
/// Carregar o protocolo com os alvos e monta-lo em resposta e o que todo caso de uso de protocolo
/// faz na entrada e na saida. Extensao estatica, e nao um servico: nao guarda estado nem precisa de
/// registro no contentor, e batiza-la <c>...Handler</c> a faria parecer um ponto de entrada.
/// </summary>
internal static class ProtocolProjection
{
    internal static async Task<Protocol> LoadProtocolAsync(
        this ISmartGraoDbContext db, Guid id, CancellationToken cancellationToken)
    {
        var protocolId = new ProtocolId(id);

        return await db.Protocols.Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == protocolId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.NotFound);
    }

    internal static async Task<ProtocolViewModel> ProjectAsync(
        this ISmartGraoDbContext db, Protocol protocol, CancellationToken cancellationToken)
    {
        var targetIds = protocol.Items.Select(item => item.TargetId).Distinct().ToList();

        var targets = await db.MonitoringTargets.AsNoTracking()
            .Where(target => targetIds.Contains(target.Id))
            .ToDictionaryAsync(target => target.Id, cancellationToken);

        return ProtocolViewModel.From(protocol, targets);
    }
}
