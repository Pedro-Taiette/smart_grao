using FluentValidation;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

public sealed class CreateFarmCommandHandler(
    ISmartGraoDbContext dbContext,
    IValidator<SaveFarmViewModel> validator)
{
    public async Task<FarmViewModel> HandleAsync(
        SaveFarmViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var farm = Farm.Create(model.Name, model.City, model.State, model.Headquarters?.ToDomain());

        dbContext.Farms.Add(farm);
        await dbContext.SaveChangesAsync(cancellationToken);

        return FarmViewModel.From(farm);
    }
}
