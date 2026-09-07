import { defineConfig } from 'orval';

/**
 * Gera o cliente da API a partir do documento OpenAPI que o backend emite a cada build.
 *
 * A entrada e um arquivo versionado e nao um servidor no ar: gerar nao exige a API rodando, e uma
 * mudanca de contrato aparece no diff do pull request em vez de estourar no navegador.
 *
 * Nada em `src/api/generated` deve ser editado a mao — a pasta e reescrita a cada `npm run api`.
 * O acoplamento a essas funcoes fica confinado aos hooks de cada feature.
 */
export default defineConfig({
  smartgrao: {
    input: {
      target: '../openapi/SmartGrao.WebApi.json',
    },
    output: {
      // Um arquivo por tag do OpenAPI (= por controller), em vez de um modulo unico gigante.
      mode: 'tags-split',
      target: './src/api/generated/smartGrao.ts',
      schemas: './src/api/generated/model',
      client: 'react-query',
      httpClient: 'axios',
      clean: true,
      indexFiles: false,
      override: {
        // Toda chamada passa pelo nosso cliente HTTP, que e onde o erro do axios vira ApiError.
        mutator: {
          path: './src/api/httpClient.ts',
          name: 'httpClient',
        },
        // Sem forcar useQuery/useMutation aqui: o Orval ja decide pelo metodo HTTP (GET vira
        // query, o resto vira mutation). Declarar os dois inverte a escolha e produz um
        // `useMutation` para listar fazendas e um `useQuery` para exclui-las.
        query: {
          // Repassa o AbortSignal do TanStack Query, para que consulta abandonada seja cancelada.
          signal: true,
        },
      },
    },
  },
});
