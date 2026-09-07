using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Sampling;

public readonly record struct SamplingPlanId(Guid Value) : IStronglyTypedId
{
    public static SamplingPlanId New() => new(Guid.CreateVersion7());
}
