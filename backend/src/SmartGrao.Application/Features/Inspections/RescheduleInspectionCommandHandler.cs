using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.People;

namespace SmartGrao.Application.Features.Inspections;

public sealed class RescheduleInspectionCommandHandler(
    ISmartGraoDbContext db, IValidator<RescheduleInspectionViewModel> validator)
{
    public async Task<InspectionViewModel> HandleAsync(
        Guid id, RescheduleInspectionViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var inspection = await db.LoadInspectionAsync(id, cancellationToken);

        var responsibleId = new PersonId(model.ResponsibleId);
        var responsible = await db.People.FirstOrDefaultAsync(x => x.Id == responsibleId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.PersonNotFound);

        inspection.Reschedule(responsible, model.ScheduledFor);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}
