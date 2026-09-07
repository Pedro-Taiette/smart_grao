using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class DeleteFieldCommandHandler(ISmartGraoDbContext dbContext)
{
    public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fieldId = new FieldId(id);

        var field = await dbContext.Fields.FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);

        dbContext.Fields.Remove(field);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
