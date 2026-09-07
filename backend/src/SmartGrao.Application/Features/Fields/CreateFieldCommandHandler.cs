using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class CreateFieldCommandHandler(
    ISmartGraoDbContext dbContext,
    FieldPlacementGuard placementGuard,
    IValidator<CreateFieldViewModel> validator)
{
    public async Task<FieldViewModel> HandleAsync(
        CreateFieldViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var farmId = new FarmId(model.FarmId);

        if (!await dbContext.Farms.AnyAsync(farm => farm.Id == farmId, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.NotFound);

        // Converte antes de qualquer consulta espacial: um poligono invalido deve custar zero ida
        // ao banco.
        var boundary = model.Boundary.ToBoundary();

        await placementGuard.EnsureNameIsAvailableAsync(farmId, model.Name, null, cancellationToken);
        await placementGuard.EnsureNoOverlapAsync(farmId, boundary, null, cancellationToken);

        var field = Field.Create(farmId, model.Name, model.Crop, boundary);

        dbContext.Fields.Add(field);
        await dbContext.SaveChangesAsync(cancellationToken);

        return FieldViewModel.From(field);
    }
}
