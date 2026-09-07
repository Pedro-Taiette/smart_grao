using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Domain.Farms;

public readonly record struct FarmId(Guid Value) : IStronglyTypedId
{
    // v7 e nao v4: ids aleatorios espalham a insercao da chave primaria pela arvore B inteira.
    public static FarmId New() => new(Guid.CreateVersion7());
}
