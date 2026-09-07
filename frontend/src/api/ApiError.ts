import { AxiosError } from 'axios';

/**
 * Uma falha vinda da API, ja traduzida para o formato com que o resto do app trabalha.
 *
 * O backend responde `ProblemDetails` (RFC 7807) com o codigo estavel em `title` e a mensagem
 * tecnica em `detail`. O `code` e o contrato: e por ele que a UI escolhe o texto que o produtor le.
 * O `detail` esta em ingles e e para quem depura — nunca vai para a tela.
 */
export class ApiError extends Error {
  readonly code: string;
  readonly status: number;
  readonly detail?: string;

  constructor(code: string, status: number, detail?: string) {
    super(detail ?? code);
    this.name = 'ApiError';
    this.code = code;
    this.status = status;
    this.detail = detail;
  }

  /** `true` quando a falha e do lado do cliente e vale mostrar ao usuario o que corrigir. */
  get isUserFixable(): boolean {
    return this.status >= 400 && this.status < 500;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
}

/**
 * Normaliza qualquer coisa que o axios lance em um {@link ApiError}.
 *
 * Os tres casos que importam sao diferentes para o usuario e por isso nao podem colapsar num
 * "erro inesperado": a API respondeu com um problema conhecido, a API nao respondeu (rede/servidor
 * fora), ou algo quebrou dentro do proprio navegador.
 */
export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (error instanceof AxiosError) {
    const problem = error.response?.data as ProblemDetails | undefined;

    if (error.response) {
      return new ApiError(
        problem?.title ?? 'internal_error',
        error.response.status,
        problem?.detail,
      );
    }

    // Sem resposta: a requisicao saiu e nada voltou.
    return new ApiError('network.unreachable', 0, error.message);
  }

  return new ApiError('internal_error', 0, error instanceof Error ? error.message : undefined);
}
