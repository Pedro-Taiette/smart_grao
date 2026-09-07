using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.WebApi.Middleware;

/// <summary>
/// Traduz excecoes nao tratadas em ProblemDetails (RFC 7807). Uma <see cref="DomainException"/>
/// carrega um <see cref="Error"/> cujo <see cref="ErrorType"/> escolhe o status; qualquer outra
/// coisa e 500.
/// <para>
/// O <c>title</c> leva o codigo estavel e o <c>detail</c> a mensagem de desenvolvedor. O frontend
/// traduz pelo codigo — ele nunca exibe o <c>detail</c> cru, que e texto tecnico em ingles.
/// </para>
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            DomainException domainException => (
                StatusPara(domainException.Error.Type),
                domainException.Error.Code,
                domainException.Error.Message),
            _ => (StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred."),
        };

        // So os 500 viram log de erro. Um 422 por poligono auto-interseccionado e o sistema
        // funcionando, e registra-lo como erro treina todo mundo a ignorar o log de erros.
        if (status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Excecao nao tratada.");

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail },
        });
    }

    private static int StatusPara(ErrorType tipo) => tipo switch
    {
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status400BadRequest,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.ExternalService => StatusCodes.Status502BadGateway,
        _ => StatusCodes.Status500InternalServerError,
    };
}
