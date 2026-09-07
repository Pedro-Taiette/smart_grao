using FluentValidation;
using SmartGrao.Domain.Abstractions;

namespace SmartGrao.Application.Common;

/// <summary>
/// Liga o FluentValidation ao modelo de erro do dominio: uma validacao que falha vira
/// <see cref="DomainException"/> do tipo <see cref="ErrorType.Validation"/>, que a API mapeia para
/// 422. Mantem os handlers livres do tipo de excecao da biblioteca — trocar de validador nao
/// encosta em nenhum caso de uso.
/// </summary>
internal static class ValidationExtensions
{
    public static async Task ValidateAndThrowDomainAsync<T>(
        this IValidator<T> validator, T instance, CancellationToken cancellationToken = default)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (result.IsValid)
            return;

        var message = string.Join(" ", result.Errors.Select(e => e.ErrorMessage));
        throw new DomainException(SmartGraoErrors.Common.ValidationFailed(message));
    }
}
