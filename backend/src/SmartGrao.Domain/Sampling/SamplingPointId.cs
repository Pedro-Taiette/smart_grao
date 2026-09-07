using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Sampling;

public readonly record struct SamplingPointId(Guid Value) : IStronglyTypedId
{
    public static SamplingPointId New() => new(Guid.CreateVersion7());
}
