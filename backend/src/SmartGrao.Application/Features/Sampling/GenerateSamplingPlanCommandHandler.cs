using SmartGrao.Domain.Cultivations;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

public sealed class GenerateSamplingPlanCommandHandler(
    ISmartGraoDbContext dbContext,
    IValidator<GenerateSamplingPlanViewModel> validator)
{
    public async Task<SamplingPlanViewModel> HandleAsync(
        GenerateSamplingPlanViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var fieldId = new FieldId(model.FieldId);

        var field = await dbContext.Fields
            .FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);

        // Um talhao desativado saiu de operacao; gerar uma caminhada para ele mandaria alguem a
        // campo por uma area que ninguem esta manejando.
        if (!field.Active)
            throw new DomainException(SmartGraoErrors.Sampling.FieldIsInactive);

        var cultivationId = new CultivationId(model.CultivationId);
        var cultivation = await dbContext.Cultivations.FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        if (cultivation.FieldId != field.Id) throw new DomainException(SmartGraoErrors.Cultivation.WrongField);
        if (cultivation.EndedOn.HasValue) throw new DomainException(SmartGraoErrors.Cultivation.Closed);
        if (model.Mode == SamplingMode.Monitoring && cultivation.Crop != Crop.Soybean)
            throw new DomainException(SmartGraoErrors.Cultivation.UnsupportedMonitoring);

        var plan = model.Mode switch
        {
            SamplingMode.Monitoring =>
                SamplingPlan.ForMonitoring(field.Id, field.Boundary, cultivation.Id),

            SamplingMode.Mapping =>
                SamplingPlan.ForMapping(
                    field.Id, field.Boundary, SamplingSpacing.Of(model.SpacingMeters!.Value), cultivation.Id),

            _ => throw new DomainException(SmartGraoErrors.Sampling.UnknownMode),
        };

        dbContext.SamplingPlans.Add(plan);
        await dbContext.SaveChangesAsync(cancellationToken);

        return SamplingPlanViewModel.From(plan);
    }
}
