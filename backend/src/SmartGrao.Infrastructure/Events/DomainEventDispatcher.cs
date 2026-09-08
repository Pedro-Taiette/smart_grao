using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SmartGrao.Application.Abstractions;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Infrastructure.Events;

/// <summary>
/// Entrega cada evento aos handlers registrados para o seu tipo concreto.
/// <para>
/// O evento chega como <see cref="IDomainEvent"/>, mas o handler e registrado como
/// <c>IDomainEventHandler&lt;FieldRedrawnEvent&gt;</c> — entao o tipo fechado precisa ser montado em
/// tempo de execucao para pedi-lo ao container. O <c>MethodInfo</c> de cada tipo de evento fica em
/// cache: sem isso, cada salvamento pagaria uma busca por reflexao.
/// </para>
/// </summary>
internal sealed class DomainEventDispatcher(IServiceProvider services) : IDomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> Dispatchers = new();

    private static readonly MethodInfo DispatchDefinition =
        typeof(DomainEventDispatcher).GetMethod(nameof(DispatchTyped), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var dispatch = Dispatchers.GetOrAdd(
                domainEvent.GetType(),
                type => DispatchDefinition.MakeGenericMethod(type));

            await (Task)dispatch.Invoke(this, [domainEvent, cancellationToken])!;
        }
    }

    private async Task DispatchTyped<TEvent>(TEvent domainEvent, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        // Um evento sem ouvinte nao e erro: a maioria dos eventos existe para ser escutado mais
        // tarde, por uma fase que ainda nao chegou.
        foreach (var handler in services.GetServices<IDomainEventHandler<TEvent>>())
        {
            await handler.HandleAsync(domainEvent, cancellationToken);
        }
    }
}
