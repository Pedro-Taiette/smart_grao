using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Cultivations;

namespace SmartGrao.Application.Features.Cultivations;

public sealed class UpdateRiskFactorsCommandHandler(ISmartGraoDbContext db, IValidator<CercosporaRiskFactorsViewModel> validator)
{
    public async Task<CultivationViewModel> HandleAsync(Guid id, CercosporaRiskFactorsViewModel model, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);
        var cultivationId = new CultivationId(id);
        var cultivation = await db.Cultivations.Include(x => x.Stages)
            .FirstOrDefaultAsync(x => x.Id == cultivationId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Cultivation.NotFound);
        cultivation.UpdateRiskFactors(model.ToDomain());
        await db.SaveChangesAsync(cancellationToken);
        return CultivationViewModel.From(cultivation);
    }
}
