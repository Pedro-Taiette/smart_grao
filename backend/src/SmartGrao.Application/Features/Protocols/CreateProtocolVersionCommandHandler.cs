using SmartGrao.Application.Abstractions;

namespace SmartGrao.Application.Features.Protocols;

/// <summary>
/// Abre a proxima versao de um protocolo publicado, copiando os alvos. E o unico caminho para mudar
/// o que ja esta em uso: a versao publicada segue intacta para as vistorias que a referenciam.
/// </summary>
public sealed class CreateProtocolVersionCommandHandler(ISmartGraoDbContext db)
{
    public async Task<ProtocolViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var published = await db.LoadProtocolAsync(id, cancellationToken);

        var next = published.CreateNextVersion();

        db.Protocols.Add(next);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(next, cancellationToken);
    }
}
