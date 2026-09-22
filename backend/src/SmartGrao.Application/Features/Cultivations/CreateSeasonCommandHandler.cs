using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class CreateSeasonCommandHandler(ISmartGraoDbContext db, IValidator<CreateSeasonViewModel> validator)
{
    public async Task<SeasonViewModel> HandleAsync(CreateSeasonViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);
        var farmId = new FarmId(model.FarmId);
        if (!await db.Farms.AnyAsync(x => x.Id == farmId, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.NotFound);
        var name = model.Name.Trim();
        if (await db.Seasons.AnyAsync(x => x.FarmId == farmId && x.Name == name, cancellationToken))
            throw new DomainException(SmartGraoErrors.Cultivation.DuplicateSeason);
        var season = Season.Create(farmId, name);
        db.Seasons.Add(season);
        await db.SaveChangesAsync(cancellationToken);
        return SeasonViewModel.From(season);
    }
}
