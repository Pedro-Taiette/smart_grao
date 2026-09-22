using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Domain.Cultivations;

public readonly record struct SeasonId(Guid Value) : IStronglyTypedId
{
    public static SeasonId New() => new(Guid.CreateVersion7());
}

/// <summary>A named agricultural season shared by the fields of one farm.</summary>
public sealed class Season : AggregateRoot<SeasonId>
{
    public const int MaximumNameLength = 80;
    private Season() { }
    public FarmId FarmId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Season Create(FarmId farmId, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaximumNameLength)
            throw new DomainException(SmartGraoErrors.Cultivation.InvalidSeasonName);
        return new Season { Id = SeasonId.New(), FarmId = farmId, Name = name.Trim() };
    }
}
