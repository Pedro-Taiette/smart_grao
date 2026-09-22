using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class CreateCultivationCommandHandler(ISmartGraoDbContext db, IValidator<CreateCultivationViewModel> validator)
{
    public async Task<CultivationViewModel> HandleAsync(CreateCultivationViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);
        var fieldId = new FieldId(model.FieldId);
        var seasonId = new SeasonId(model.SeasonId);
        var field = await db.Fields.FirstOrDefaultAsync(x => x.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);
        var season = await db.Seasons.FirstOrDefaultAsync(x => x.Id == seasonId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.SeasonNotFound);
        if (!field.Active) throw new DomainException(SmartGraoErrors.Sampling.FieldIsInactive);
        if (season.FarmId != field.FarmId) throw new DomainException(SmartGraoErrors.Cultivation.WrongFarm);
        // The new cycle has no end date: any existing interval ending on/after its start overlaps.
        if (await db.Cultivations.AnyAsync(x => x.FieldId == fieldId
            && (x.EndedOn == null || x.EndedOn >= model.PlantedOn), cancellationToken))
            throw new DomainException(SmartGraoErrors.Cultivation.OverlappingCycle);
        var cultivation = Cultivation.Create(fieldId, seasonId, model.Crop, model.Cultivar, model.PlantedOn);
        db.Cultivations.Add(cultivation);
        await db.SaveChangesAsync(cancellationToken);
        return CultivationViewModel.From(cultivation);
    }
}
