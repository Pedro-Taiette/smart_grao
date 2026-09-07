namespace SmartGrao.Domain.Abstractions;

/// <summary>
/// Raiz de uma fronteira de consistencia e o unico tipo de entidade persistido diretamente.
/// Acumula eventos de dominio que a camada de persistencia despacha depois do commit.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IHasDomainEvents
    where TId : struct, IStronglyTypedId
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public DateTimeOffset CreatedAt { get; protected set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void MarkAsUpdated() => UpdatedAt = DateTimeOffset.UtcNow;

    public void ClearDomainEvents() => _domainEvents.Clear();
}

/// <summary>
/// Permite que a persistencia limpe os eventos de qualquer agregado sem precisar conhecer o tipo
/// concreto de cada um — sem isso, o <c>SaveChangesAsync</c> teria um <c>switch</c> que cresce a
/// cada agregado novo e que alguem inevitavelmente esquece de atualizar.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    void ClearDomainEvents();
}
