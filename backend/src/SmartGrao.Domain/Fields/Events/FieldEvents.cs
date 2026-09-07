using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Farms;

namespace SmartGrao.Domain.Fields.Events;

public sealed record FieldCreatedEvent(FieldId FieldId, FarmId FarmId, decimal AreaHectares) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// O contorno mudou. O Pilar 2 escuta: a malha de amostragem gerada para o contorno antigo tem
/// pontos que podem ter caido para fora do talhao, e continuar guiando o produtor ate eles seria
/// manda-lo caminhar sobre a lavoura do vizinho.
/// </summary>
public sealed record FieldRedrawnEvent(
    FieldId FieldId,
    decimal PreviousAreaHectares,
    decimal CurrentAreaHectares) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
