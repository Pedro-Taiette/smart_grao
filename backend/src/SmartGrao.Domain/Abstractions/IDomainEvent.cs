namespace SmartGrao.Domain.Abstractions;

/// <summary>Algo relevante que aconteceu no dominio, no passado.</summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
