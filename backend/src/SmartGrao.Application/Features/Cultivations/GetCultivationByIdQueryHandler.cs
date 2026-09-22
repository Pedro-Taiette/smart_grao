using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class GetCultivationByIdQueryHandler(ISmartGraoDbContext db)
{
    public async Task<CultivationViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cultivationId = new CultivationId(id);
        var cultivation = await db.Cultivations.AsNoTracking().Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        return CultivationViewModel.From(cultivation);
    }
}
