namespace SmartGrao.Domain.Abstractions;

/// <summary>
/// Marcador para identificador fortemente tipado sobre <see cref="System.Guid"/>. Cada agregado tem
/// o seu (ex.: <c>FieldId</c>), de modo que passar um <c>FarmId</c> onde se espera um
/// <c>FieldId</c> deixa de compilar. A conversao para a coluna Guid vive na Infrastructure.
/// </summary>
public interface IStronglyTypedId
{
    Guid Value { get; }
}
