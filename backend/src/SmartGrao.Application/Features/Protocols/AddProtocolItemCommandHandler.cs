using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class AddProtocolItemCommandHandler(
    ISmartGraoDbContext db, IValidator<AddProtocolItemViewModel> validator)
{
    public async Task<ProtocolViewModel> HandleAsync(
        Guid id, AddProtocolItemViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var protocol = await db.LoadProtocolAsync(id, cancellationToken);

        var targetId = new MonitoringTargetId(model.TargetId);
        var target = await db.MonitoringTargets.FirstOrDefaultAsync(x => x.Id == targetId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.TargetNotFound);

        var referenceLevel = model.ReferenceLevel is { } level
            ? ReferenceLevel.Create(level.Threshold, level.Source)
            : null;

        protocol.AddItem(
            target, model.Organ, model.Unit, model.PhotosRequested, model.Instructions, referenceLevel);

        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(protocol, cancellationToken);
    }
}
