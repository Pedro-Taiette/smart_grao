using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Fields;

public sealed class GetFieldsByFarmQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<IReadOnlyList<FieldViewModel>> HandleAsync(
        Guid farmId, bool activeOnly = false, CancellationToken cancellationToken = default)
    {
        var id = new FarmId(farmId);

        var query = dbContext.Fields
            .AsNoTracking()
            .Where(field => field.FarmId == id);

        if (activeOnly)
            query = query.Where(field => field.Active);

        var fields = await query.OrderBy(field => field.Name).ToListAsync(cancellationToken);

        return [.. fields.Select(FieldViewModel.From)];
    }
}
