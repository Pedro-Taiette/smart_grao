using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SmartGrao.Application.Abstractions;
using SmartGrao.Application.Features.Fields;

namespace SmartGrao.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra os casos de uso por convencao (Scrutor): classes cujo nome termina em
    /// <c>CommandHandler</c> ou <c>QueryHandler</c>.
    /// <para>
    /// Sem biblioteca de mediator. O handler e injetado direto na acao do controller que o usa,
    /// entao "localizar usos" mostra quem o chama e o compilador confere os argumentos — nada disso
    /// vale para um <c>Send(new AlgumCommand(...))</c> resolvido por reflexao.
    /// </para>
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.Scan(scan => scan
            .FromAssemblyOf<AssemblyMarker>()
            .AddClasses(c => c.Where(t =>
                t.Name.EndsWith("CommandHandler", StringComparison.Ordinal) ||
                t.Name.EndsWith("QueryHandler", StringComparison.Ordinal)))
            .AsSelf()
            .WithScopedLifetime());

        // Nao e um caso de uso, entao a convencao acima nao o alcanca: e a peca que dois handlers
        // compartilham — criar e atualizar — e batiza-lo para casar com a varredura diria que ele e
        // um ponto de entrada, quando ele e o oposto disso.
        services.AddScoped<FieldPlacementGuard>();

        // Handlers de evento de dominio: registrados pela interface, e nao pelo nome, porque quem
        // os procura e o despachante — que so conhece o tipo do evento.
        services.Scan(scan => scan
            .FromAssemblyOf<AssemblyMarker>()
            .AddClasses(c => c.AssignableTo(typeof(IDomainEventHandler<>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();

        return services;
    }
}

/// <summary>Ancora para a varredura de assembly.</summary>
public sealed class AssemblyMarker;
