using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.People;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Inspections;

public sealed class ScheduleInspectionCommandHandler(
    ISmartGraoDbContext db, IValidator<ScheduleInspectionViewModel> validator)
{
    public async Task<InspectionViewModel> HandleAsync(
        ScheduleInspectionViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var cultivationId = new CultivationId(model.CultivationId);
        var cultivation = await db.Cultivations.FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);

        var planId = new SamplingPlanId(model.SamplingPlanId);
        var plan = await db.SamplingPlans.FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Sampling.NotFound);

        var protocolId = new ProtocolId(model.ProtocolId);
        var protocol = await db.Protocols.FirstOrDefaultAsync(x => x.Id == protocolId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.NotFound);

        var responsibleId = new PersonId(model.ResponsibleId);
        var responsible = await db.People.FirstOrDefaultAsync(x => x.Id == responsibleId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Inspection.PersonNotFound);

        // A pessoa e a fazenda do talhao precisam bater. O dominio nao tem como checar: o talhao nao
        // entra no agregado da vistoria, so o seu id.
        var farmId = await db.Fields.Where(x => x.Id == cultivation.FieldId)
            .Select(x => x.FarmId).FirstAsync(cancellationToken);
        if (responsible.FarmId != farmId)
            throw new DomainException(SmartGraoErrors.Inspection.PersonFromAnotherFarm);

        var inspection = Inspection.Schedule(cultivation, plan, protocol, responsible, model.ScheduledFor);

        db.Inspections.Add(inspection);
        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}
