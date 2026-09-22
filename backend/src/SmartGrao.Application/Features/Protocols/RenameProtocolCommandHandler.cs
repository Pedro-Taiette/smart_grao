using FluentValidation;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;

namespace SmartGrao.Application.Features.Protocols;

public sealed class RenameProtocolCommandHandler(
    ISmartGraoDbContext db, IValidator<RenameProtocolViewModel> validator)
{
    public async Task<ProtocolViewModel> HandleAsync(
        Guid id, RenameProtocolViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var protocol = await db.LoadProtocolAsync(id, cancellationToken);

        protocol.Rename(model.Name);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
