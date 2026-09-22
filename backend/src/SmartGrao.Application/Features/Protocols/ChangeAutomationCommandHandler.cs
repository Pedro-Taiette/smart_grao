using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class ChangeAutomationCommandHandler(
    ISmartGraoDbContext db, IValidator<ChangeAutomationViewModel> validator)
{
    public async Task<MonitoringTargetViewModel> HandleAsync(
        Guid id, ChangeAutomationViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var targetId = new MonitoringTargetId(id);
        var target = await db.MonitoringTargets.FirstOrDefaultAsync(x => x.Id == targetId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.TargetNotFound);

        target.ChangeAutomation(model.Automation);
        await db.SaveChangesAsync(cancellationToken);

        return MonitoringTargetViewModel.From(target);
    }
}
