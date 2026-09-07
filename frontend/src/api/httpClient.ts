import axios, { type AxiosRequestConfig } from 'axios';
import { toApiError } from './ApiError';

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5148';

const instance = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
});

/**
 * O mutator do Orval: todo hook gerado passa por aqui.
 *
 * E o unico ponto onde o app fala HTTP. Por isso e aqui que a falha do axios vira {@link ApiError}
 * — nenhum componente, hook de feature ou tela precisa saber que existe axios, `response.data` ou
 * status HTTP.
 *
 * O `signal` vem do TanStack Query e e repassado ao axios, entao uma consulta abandonada (o usuario
 * trocou de tela) cancela a requisicao em vez de deixa-la terminar e descartar o resultado.
 */
export const httpClient = async <T>(
  config: AxiosRequestConfig,
  options?: AxiosRequestConfig,
): Promise<T> => {
  try {
    const response = await instance({ ...config, ...options });
    return response.data as T;
  } catch (error) {
    throw toApiError(error);
  }
};

export default httpClient;
