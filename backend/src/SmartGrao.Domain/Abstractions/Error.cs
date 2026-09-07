namespace SmartGrao.Domain.Abstractions;

/// <summary>Natureza da falha; e o que escolhe o status HTTP na borda da API.</summary>
public enum ErrorType
{
    Validation,   // 422
    NotFound,     // 404
    Conflict,     // 409
    BusinessRule, // 400
    Forbidden,    // 403

    /// <summary>
    /// Algo de que dependemos falhou e o chamador nao errou nada. 502 e nao 500 porque as duas
    /// coisas dizem o oposto a quem esta olhando: 500 e "este sistema quebrou, repetir nao ajuda";
    /// 502 e "o outro lado quebrou, tentar de novo mais tarde e razoavel".
    /// </summary>
    ExternalService, // 502
}

/// <summary>
/// Erro estruturado de dominio: codigo estavel, mensagem para desenvolvedor e tipo.
/// <para>
/// O <see cref="Code"/> e o contrato com o frontend, que traduz por chave. A <see cref="Message"/>
/// nunca e exibida ao produtor — ela existe para quem esta depurando.
/// </para>
/// </summary>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error ExternalService(string code, string message) => new(code, message, ErrorType.ExternalService);
}
