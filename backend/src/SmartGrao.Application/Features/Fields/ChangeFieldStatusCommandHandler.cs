using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class ChangeFieldStatusCommandHandler(ISmartGraoDbContext dbContext)
{
    public async Task<FieldViewModel> HandleAsync(
        Guid id, bool active, CancellationToken cancellationToken = default)
    {
        var fieldId = new FieldId(id);

        var field = await dbContext.Fields.FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);

        if (active)
            field.Reactivate();
        else
            field.Deactivate();

        await dbContext.SaveChangesAsync(cancellationToken);

        return FieldViewModel.From(field);
    }
}
