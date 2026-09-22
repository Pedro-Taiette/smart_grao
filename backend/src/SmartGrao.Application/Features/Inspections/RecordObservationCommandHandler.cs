using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Inspections;
using SmartGrao.Domain.Protocols;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Inspections;

/// <summary>
/// Registra uma parada.
/// <para>
/// O horário vem do cliente, e não do servidor: quem coleta pode estar sem sinal — a fase 4 trata
/// disso — e o que importa é a hora em que a pessoa esteve no ponto, não a hora em que o registro
/// conseguiu subir.
/// </para>
/// </summary>
public sealed class RecordObservationCommandHandler(
    ISmartGraoDbContext db, IValidator<RecordObservationViewModel> validator)
{
    public async Task<InspectionViewModel> HandleAsync(
        Guid id, RecordObservationViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var inspection = await db.LoadInspectionAsync(id, cancellationToken);

        var protocolId = inspection.ProtocolId;
        var protocol = await db.Protocols.AsNoTracking().Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == protocolId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Protocol.NotFound);

        var items = protocol.Items.ToDictionary(item => item.Id);
        var counts = new List<ObservationCount>(model.Counts.Count);
        foreach (var count in model.Counts)
        {
            if (!items.TryGetValue(new ProtocolItemId(count.ProtocolItemId), out var item))
                throw new DomainException(SmartGraoErrors.Inspection.CountFromAnotherProtocol);

            counts.Add(new ObservationCount(item, count.Value, count.Detected));
        }

        SamplingPoint? point = null;
        if (model.SamplingPointId is { } rawPointId)
        {
            var planId = inspection.SamplingPlanId;
            var plan = await db.SamplingPlans.AsNoTracking().Include(x => x.Points)
                .FirstOrDefaultAsync(x => x.Id == planId, cancellationToken)
                ?? throw new DomainException(SmartGraoErrors.Sampling.NotFound);

            var pointId = new SamplingPointId(rawPointId);
            point = plan.Points.FirstOrDefault(x => x.Id == pointId)
                ?? throw new DomainException(SmartGraoErrors.Inspection.PointFromAnotherPlan);
        }

        inspection.RecordObservation(
            point, model.RecordedAt, model.Location.ToDomain(), model.AccuracyMeters,
            model.GrowthStage, model.Notes, counts);

        await db.SaveChangesAsync(cancellationToken);

        return await db.ProjectAsync(inspection, cancellationToken);
    }
}
