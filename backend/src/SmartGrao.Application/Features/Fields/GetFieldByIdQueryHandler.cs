using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Application.Features.Fields;

public sealed class GetFieldByIdQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<FieldViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fieldId = new FieldId(id);

        var field = await dbContext.Fields
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fieldId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Field.NotFound);

        return FieldViewModel.From(field);
    }
}
