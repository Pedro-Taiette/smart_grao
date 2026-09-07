namespace SmartGrao.Domain.Abstractions;

/// <summary>
/// Base de entidade identificada por id fortemente tipado. A igualdade e por id + tipo concreto,
/// entao duas entidades de tipos diferentes que compartilhem o mesmo Guid nunca sao iguais.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : struct, IStronglyTypedId
{
    public TId Id { get; protected init; }

    public bool Equals(Entity<TId>? other) =>
        other is not null && other.GetType() == GetType() && other.Id.Value.Equals(Id.Value);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id.Value);
}
