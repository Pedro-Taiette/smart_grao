using FluentValidation;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Protocols;

namespace SmartGrao.Application.Features.Protocols;

public sealed class CreateMonitoringTargetCommandHandler(
    ISmartGraoDbContext db, IValidator<CreateMonitoringTargetViewModel> validator)
{
    public async Task<MonitoringTargetViewModel> HandleAsync(
        CreateMonitoringTargetViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var target = MonitoringTarget.Create(
            model.Code, model.CommonName, model.ScientificName, model.Kind, model.Crop);

        db.MonitoringTargets.Add(target);
        await db.SaveChangesAsync(cancellationToken);

        return MonitoringTargetViewModel.From(target);
    }
}
