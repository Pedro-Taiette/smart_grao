using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Common;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class UpdateFieldCommandHandler(
    ISmartGraoDbContext dbContext,
    FieldPlacementGuard placementGuard,
    IValidator<UpdateFieldViewModel> validator)
{
    public async Task<FieldViewModel> HandleAsync(
        Guid id, UpdateFieldViewModel model, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        await validator.ValidateAndThrowDomainAsync(model, cancellationToken);

        var fieldId = new FieldId(id);

        var field = await dbContext.Fields.FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);

        var boundary = model.Boundary.ToBoundary();

        await placementGuard.EnsureNameIsAvailableAsync(field.FarmId, model.Name, fieldId, cancellationToken);
        await placementGuard.EnsureNoOverlapAsync(field.FarmId, boundary, fieldId, cancellationToken);

        field.Rename(model.Name);
        field.ChangeCrop(model.Crop);
        field.Redraw(boundary);

        await dbContext.SaveChangesAsync(cancellationToken);

        return FieldViewModel.From(field);
    }
}
