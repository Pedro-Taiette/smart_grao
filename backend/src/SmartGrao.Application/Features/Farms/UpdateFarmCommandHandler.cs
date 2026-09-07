using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

public sealed class UpdateFarmCommandHandler(
    ISmartGraoDbContext dbContext,
    IValidator<SaveFarmViewModel> validator)
{
    public async Task<FarmViewModel> HandleAsync(
        Guid id, SaveFarmViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var farmId = new FarmId(id);

        var farm = await dbContext.Farms.FirstOrDefaultAsync(f => f.Id == farmId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Farm.NotFound);

        farm.Update(model.Name, model.City, model.State);
        farm.SetHeadquarters(model.Headquarters?.ToDomain());

        await dbContext.SaveChangesAsync(cancellationToken);

        return FarmViewModel.From(farm);
    }
}
