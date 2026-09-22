using FluentValidation;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class CreateProtocolCommandHandler(
    ISmartGraoDbContext db, IValidator<CreateProtocolViewModel> validator)
{
    public async Task<ProtocolViewModel> HandleAsync(
        CreateProtocolViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var protocol = Protocol.CreateDraft(model.Code, model.Crop, model.Name);

        db.Protocols.Add(protocol);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
