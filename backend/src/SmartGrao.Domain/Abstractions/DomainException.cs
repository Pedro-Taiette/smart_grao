namespace SmartGrao.Domain.Abstractions;

/// <summary>
/// Lancada quando uma invariante e violada. Carrega um <see cref="Error"/> estruturado para que a
/// borda da API escolha o status certo sem uma cadeia de catch.
/// <para>
/// E o unico canal de falha: nao ha tipo <c>Result</c>. Uma invariante que nunca pode produzir um
/// objeto invalido e guardada onde o objeto e construido — a fabrica ou o objeto de valor — para
/// que nenhum chamador consiga pular a checagem esquecendo de inspecionar um retorno.
/// </para>
/// </summary>
public sealed class DomainException(Error error, Exception? innerException = null)
    : Exception(error.Message, innerException)
{
    public Error Error { get; } = error;
}
