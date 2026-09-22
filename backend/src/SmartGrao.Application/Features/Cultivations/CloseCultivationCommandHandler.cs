using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class CloseCultivationCommandHandler(ISmartGraoDbContext db)
{
    public async Task<CultivationViewModel> HandleAsync(Guid id, CloseCultivationViewModel model, CancellationToken cancellationToken = default)
    {
        var cultivationId = new CultivationId(id);
        var cultivation = await db.Cultivations.Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        cultivation.Close(model.EndedOn);
        await db.SaveChangesAsync(cancellationToken);
        return CultivationViewModel.From(cultivation);
    }
}
