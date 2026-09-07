using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

public sealed class GetFarmByIdQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<FarmViewModel> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var farmId = new FarmId(id);

        var farm = await dbContext.Farms
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == farmId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Farm.NotFound);

        return FarmViewModel.From(farm);
    }
}
