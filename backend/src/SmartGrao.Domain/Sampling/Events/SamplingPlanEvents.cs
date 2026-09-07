using SmartGrao.Domain.Abstractions;
using SmartGrao.Domain.Fields;

namespace SmartGrao.Domain.Sampling.Events;

/// <summary>
/// Uma malha nova foi gerada para um talhao. O Pilar 3 escuta na Fase 4: e o gatilho para preparar a
/// coleta em campo e, mais tarde, o voo do drone sobre os mesmos pontos.
/// </summary>
public sealed record SamplingPlanGeneratedEvent(
    SamplingPlanId SamplingPlanId,
    FieldId FieldId,
    SamplingMode Mode,
    int PointCount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
