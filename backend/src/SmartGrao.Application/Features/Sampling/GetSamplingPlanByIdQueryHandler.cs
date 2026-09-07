using Microsoft.EntityFrameworkCore;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Sampling;

namespace SmartGrao.Application.Features.Sampling;

public sealed class GetSamplingPlanByIdQueryHandler(ISmartGraoDbContext dbContext)
{
    public async Task<SamplingPlanViewModel> HandleAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        var planId = new SamplingPlanId(id);

        var plan = await dbContext.SamplingPlans
            .AsNoTracking()
            .Include(plan => plan.Points)
            .FirstOrDefaultAsync(plan => plan.Id == planId, cancellationToken)
            ?? throw new DomainException(SmartGraoErrors.Sampling.NotFound);

        return SamplingPlanViewModel.From(plan);
    }
}
