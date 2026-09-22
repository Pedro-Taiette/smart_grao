using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class GetCultivationsQueryHandler(ISmartGraoDbContext db)
{
    public async Task<IReadOnlyList<CultivationViewModel>> HandleAsync(Guid fieldId, CancellationToken cancellationToken = default)
    {
        var id = new FieldId(fieldId);
        if (!await db.Fields.AnyAsync(x => x.Id == id, cancellationToken))
            throw new DomainException(SmartGraoErrors.Field.NotFound);
        var cultivations = await db.Cultivations.AsNoTracking().Include(x => x.Stages)
            .Where(x => x.FieldId == id).OrderByDescending(x => x.PlantedOn).ToListAsync(cancellationToken);
        return cultivations.Select(CultivationViewModel.From).ToList();
    }
}
