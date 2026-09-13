using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Application.Abstractions;

/// <summary>
/// Reage a um evento de dominio.
/// <para>
/// Ao contrario dos casos de uso, que sao injetados direto na acao do controller que os chama, o
/// handler de evento e resolvido por tipo: quem publica o evento e o agregado, e ele nao conhece
/// nem pode conhecer quem escuta. E a unica indirecao por reflexao do projeto, e existe porque a
/// alternativa — o agregado chamando o ouvinte — seria o acoplamento que o evento existe para
/// evitar.
/// </para>
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}

/// <summary>
/// Entrega os eventos acumulados pelos agregados a quem escuta.
/// <para>
/// A entrega acontece <b>antes</b> do commit, na mesma transacao. Handler de evento aqui existe
/// para manter o modelo coerente — marcar como defasada a malha de um talhao que foi redesenhado —,
/// e nao para efeito externo como e-mail. Entregar depois do commit deixaria uma janela em que o
/// contorno ja mudou e a malha ainda se diz atual, e se a segunda transacao falhasse a janela
/// viraria permanente.
/// </para>
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
