using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Application.Features.Farms;

public sealed class DeleteFarmCommandHandler(ISmartGraoDbContext dbContext)
{
    public async Task HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var farmId = new FarmId(id);

        var farm = await dbContext.Farms.FirstOrDefaultAsync(f => f.Id == farmId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Farm.NotFound);

        // Recusa em vez de apagar em cascata. Os talhoes carregam o historico de amostragem e
        // diagnostico da lavoura; um clique que os levasse junto seria irreversivel e silencioso.
        if (await dbContext.Fields.AnyAsync(f => f.FarmId == farmId, cancellationToken))
            throw new DomainException(SmartGraoErrors.Farm.HasFields);

        dbContext.Farms.Remove(farm);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
