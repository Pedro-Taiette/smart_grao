using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Fields;

public readonly record struct FieldId(Guid Value) : IStronglyTypedId
{
    public static FieldId New() => new(Guid.CreateVersion7());
}
