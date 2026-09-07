using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;

namespace SmartGrao.Application.Features.Farms;

public sealed class GetFarmsQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<IReadOnlyList<FarmViewModel>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var farms = await dbContext.Farms
            .AsNoTracking()
            .OrderBy(farm => farm.Name)
            .ToListAsync(cancellationToken);

        return [.. farms.Select(FarmViewModel.From)];
    }
}
