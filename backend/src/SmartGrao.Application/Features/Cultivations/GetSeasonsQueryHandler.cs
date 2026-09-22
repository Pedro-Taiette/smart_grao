using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class GetSeasonsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<SeasonViewModel>> HandleAsync(Guid farmId, CancellationToken cancellationToken = default)
    {
        var id = new FarmId(farmId);
        if (!await db.Farms.AnyAsync(x => x.Id == id, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.NotFound);
        var seasons = await db.Seasons.AsNoTracking().Where(x => x.FarmId == id)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
        return seasons.Select(SeasonViewModel.From).ToList();
    }
}
